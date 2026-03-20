using Core.Sampling;
using Engine.Integrators;
using Engine.Rendering;
using ScriptApi;

namespace UI.Rendering;

/// <summary>
/// Renders a <see cref="SceneDescription"/> progressively on a background
/// thread, firing a progress callback after each tile completes.
/// The callback is invoked on the thread pool — callers are responsible
/// for marshalling to the UI thread if needed.
/// </summary>
public sealed class ProgressiveRenderer
{
    /// <summary>Tile width and height in pixels.</summary>
    public int TileSize { get; init; } = 16;

    /// <summary>
    /// Renders the scene asynchronously, reporting progress after each
    /// tile completes.
    /// </summary>
    /// <param name="scene">
    /// The fully built scene description from the ScriptApi.
    /// </param>
    /// <param name="onProgress">
    /// Callback invoked after each tile completes. Called on a thread
    /// pool thread — marshal to the UI thread if needed.
    /// </param>
    /// <param name="cancellationToken">
    /// Token to cancel the render. The task completes cleanly on
    /// cancellation without throwing.
    /// </param>
    /// <returns>
    /// The completed frame buffer, or the partially rendered frame buffer
    /// if cancelled.
    /// </returns>
    public Task<FrameBuffer> RenderAsync(
        SceneDescription scene,
        Action<RenderProgress>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() =>
            Render(scene, onProgress, cancellationToken),
            cancellationToken);
    }

    private FrameBuffer Render(
        SceneDescription scene,
        Action<RenderProgress>? onProgress,
        CancellationToken cancellationToken)
    {
        var width = scene.Settings.ImageWidth;
        var height = scene.Settings.ImageHeight;
        var fb = new FrameBuffer(width, height);

        var tilesX = (int)Math.Ceiling((double)width / TileSize);
        var tilesY = (int)Math.Ceiling((double)height / TileSize);
        var totalTiles = tilesX * tilesY;
        var tilesCompleted = 0;

        var integrator = new MisIntegrator
        {
            BackgroundRadiance = scene.Settings.BackgroundRadiance
        };

        var sw = System.Diagnostics.Stopwatch.StartNew();

        var tiles = new List<(int tx, int ty)>(totalTiles);
        for (var ty = 0; ty < tilesY; ty++)
            for (var tx = 0; tx < tilesX; tx++)
                tiles.Add((tx, ty));

        // Fisher-Yates shuffle for uniform random ordering
        var rng = new Random();
        for (var i = tiles.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (tiles[i], tiles[j]) = (tiles[j], tiles[i]);
        }

        try
        {
            Parallel.ForEach(
                tiles,
                new ParallelOptions { CancellationToken = cancellationToken },
                tile =>
                {
                    if (cancellationToken.IsCancellationRequested)
                        return;

                    var (tx, ty) = tile;
                    var sampler = new Sampler(seed: HashSeed(tx, ty));

                    RenderTile(tx, ty, scene, fb, integrator, sampler);

                    var completed = Interlocked.Increment(ref tilesCompleted);

                    onProgress?.Invoke(new RenderProgress(
                        completed,
                        totalTiles,
                        fb,
                        sw.Elapsed));
                });
        }
        catch (OperationCanceledException)
        {
            // Render was cancelled — return partial result
        }

        return fb;
    }

    private void RenderTile(
        int tx, int ty,
        SceneDescription scene,
        FrameBuffer frameBuffer,
        MisIntegrator integrator,
        Sampler sampler)
    {
        var x0 = tx * TileSize;
        var y0 = ty * TileSize;
        var x1 = Math.Min(x0 + TileSize, frameBuffer.Width);
        var y1 = Math.Min(y0 + TileSize, frameBuffer.Height);

        for (var y = y0; y < y1; y++)
            for (var x = x0; x < x1; x++)
                for (var s = 0; s < scene.Settings.SamplesPerPixel; s++)
                {
                    var ray = scene.Camera.GenerateRay(
                        x, y,
                        sampler.Next(),
                        sampler.Next(),
                        sampler);

                    var radiance = integrator.Trace(
                        ray,
                        scene.Scene,
                        scene.Lights,
                        sampler);

                    frameBuffer.AddSample(x, y, radiance);
                }
    }

    /// <summary>
    /// Produces a well-distributed seed for a tile at (tx, ty).
    /// Uses a hash to avoid correlation between adjacent tiles.
    /// </summary>
    private static int HashSeed(int tx, int ty)
    {
        unchecked
        {
            int hash = (int)2166136261;
            hash = (hash ^ tx) * 16777619;
            hash = (hash ^ ty) * 16777619;
            return hash;
        }
    }
}