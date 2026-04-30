using System.Diagnostics;
using Core.Camera;
using Core.Math;
using Core.Random;
using Core.Sampling;

namespace Core.Rendering;

/// <summary>
/// Progressive tile-based renderer that accumulates into an AccumulationBuffer.
/// Partial image is always valid because each pixel maintains its own sample count.
/// Abortable via CancellationToken.
/// </summary>
public static class ProgressiveRenderer
{
    private readonly struct Tile
    {
        public readonly int X0, X1, Y0, Y1;
        public Tile(int x0, int x1, int y0, int y1) => (X0, X1, Y0, Y1) = (x0, x1, y0, y1);
    }

    public static async Task RenderLoopAsync(
        int width,
        int height,
        ICamera camera,
        Scene.Scene scene,
        AccumulationBuffer accum,
        int tileSize,
        ulong baseSeed,
        CancellationToken token,
        Action<RenderProgress>? reportProgress,
        Action<int, int, int, int>? reportTileUpdated, // (x0,y0,w,h) for UI dirty-rect updates
        int? maxDegreeOfParallelism = null,
        int progressEveryNTiles = 8,
        int? targetSpp = null)
    {
        if (accum.Width != width || accum.Height != height)
            throw new ArgumentException("AccumulationBuffer size mismatch");

        var tiles = BuildTiles(width, height, tileSize);
        int tilesTotal = tiles.Count;

        var options = new ParallelOptions
        {
            CancellationToken = token,
            MaxDegreeOfParallelism = maxDegreeOfParallelism ?? Environment.ProcessorCount
        };

        var sw = Stopwatch.StartNew();
        long totalSamples = 0;

        // Progressive loop: keep rendering passes until cancelled OR target spp reached
        while (!token.IsCancellationRequested)
        {
            int tilesDoneThisPass = 0;
            double passStart = sw.Elapsed.TotalSeconds;

            Parallel.ForEach(tiles, options, tile =>
            {
                options.CancellationToken.ThrowIfCancellationRequested();

                for (int y = tile.Y0; y < tile.Y1; y++)
                {
                    for (int x = tile.X0; x < tile.X1; x++)
                    {
                        int sampleIndex = accum.GetSpp(x, y);

                        ulong seed = SeedHash.PixelSampleSeed(x, y, sampleIndex, baseSeed);
                        var rng = new Pcg32(seed);
                        var sampler = new Sampler(rng);

                        float u = (x + sampler.Next1D()) / width;
                        float v = (y + sampler.Next1D()) / height;

                        var ray = camera.GetRay(u, 1f - v, sampler);

                        Vec3 c = PathTracerSingleSample(ray, scene, sampler);

                        accum.AddSample(x, y, c);
                    }
                }

                reportTileUpdated?.Invoke(tile.X0, tile.Y0, tile.X1 - tile.X0, tile.Y1 - tile.Y0);

                int done = Interlocked.Increment(ref tilesDoneThisPass);

                if (reportProgress != null && (done % progressEveryNTiles == 0 || done == tilesTotal))
                {
                    int sppMin = accum.GetSppMinMax(out int sppMax);
                    float avgLum = accum.ComputeAverageLuminance();

                    double elapsed = sw.Elapsed.TotalSeconds;
                    double msPerTile = (sw.Elapsed.TotalSeconds - passStart) > 0
                        ? ((sw.Elapsed.TotalSeconds - passStart) * 1000.0) / done
                        : 0;

                    double sppPerSec = elapsed > 0 ? totalSamples / elapsed : 0;

                    reportProgress(new RenderProgress(
                        Width: width,
                        Height: height,
                        SamplesPerPixelMin: sppMin,
                        SamplesPerPixelMax: sppMax,
                        TilesDone: done,
                        TilesTotal: tilesTotal,
                        ElapsedSeconds: elapsed,
                        SamplesPerSecond: sppPerSec,
                        MsPerTile: msPerTile,
                        AverageLuminance: avgLum));
                }
            });

            // Completed one full pass => every pixel should have +1 spp (unless cancelled mid-pass)
            totalSamples += (long)width * height;

            // Stop automatically when we reached target spp for all pixels
            if (targetSpp.HasValue)
            {
                int sppMin = accum.GetSppMinMax(out _);
                if (sppMin >= targetSpp.Value)
                    break;
            }

            await Task.Yield();
        }
    }

    private static List<Tile> BuildTiles(int width, int height, int tileSize)
    {
        if (tileSize <= 0) tileSize = 16;

        int tilesX = (width + tileSize - 1) / tileSize;
        int tilesY = (height + tileSize - 1) / tileSize;

        var tiles = new List<Tile>(tilesX * tilesY);

        for (int y0 = 0; y0 < height; y0 += tileSize)
        {
            int y1 = System.Math.Min(y0 + tileSize, height);
            for (int x0 = 0; x0 < width; x0 += tileSize)
            {
                int x1 = System.Math.Min(x0 + tileSize, width);
                tiles.Add(new Tile(x0, x1, y0, y1));
            }
        }

        return tiles;
    }

    /// <summary>
    /// One-sample evaluation. This calls into your existing path tracer logic.
    /// We keep it isolated so Milestone 10 doesn't require rewriting your whole renderer.
    ///
    /// Implementation strategy:
    /// - Use the same integrator used by PathTracer.Render, but for a single ray.
    /// - If your current PathTracer already has a Li method, expose an internal helper.
    ///
    /// For now, implement this by calling PathTracer.EvaluateRay(...) (added below).
    /// </summary>
    private static Vec3 PathTracerSingleSample(in Ray ray, Scene.Scene scene, Sampler sampler)
        => PathTracer.EvaluateRay(ray, scene, sampler);
}