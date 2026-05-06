using System.Diagnostics;
using Core.Camera;
using Core.Debugging;
using Core.Math;
using Core.Rendering;

namespace Core.PhotonMapping.Sppm;

public static class SppmRunner12_3
{
    public static void RunIteration(
        int width,
        int height,
        ICamera camera,
        Scene.Scene scene,
        DebugBufferSet dbg,
        AccumulationBuffer beauty,
        Dictionary<int, VisiblePoint> persistentVps,
        ulong baseSeed,
        int iterationIndex,
        int photonsPerPass,
        int photonMaxDepth,
        float initialRadius,
        float alpha,
        out SppmIterationStats stats)
    {
        stats = new SppmIterationStats();
        var sw = Stopwatch.StartNew();

        // --- 1) Eye pass: generate visible points (reusing persistent state) ---
        var currentVps = EyePass(
            width, height,
            camera, scene,
            baseSeed, iterationIndex,
            initialRadius,
            persistentVps,
            dbg,
            stats);

        stats.EyePassMs = sw.Elapsed.TotalMilliseconds;

        // --- 2) Build grid ---
        var grid = new VisiblePointGrid(cellSize: initialRadius);
        foreach (var vp in currentVps)
            grid.Insert(vp);

        // --- 3) Photon pass ---
        sw.Restart();
        var pstats = new PhotonTraceStats();

        var photons = PhotonTracer.TracePhotonPass(
            scene,
            photonsPerPass,
            photonMaxDepth,
            baseSeed,
            iterationIndex,
            pstats);

        stats.PhotonsStored = pstats.PhotonsStored;
        stats.PhotonPassMs = sw.Elapsed.TotalMilliseconds;

        // --- 4) Gather ---
        sw.Restart();
        Gather(grid, photons, stats);
        stats.GatherMs = sw.Elapsed.TotalMilliseconds;

        // --- 5) Progressive update ---
        foreach (var vp in currentVps)
        {
            SppmUpdater.Update(vp, alpha);
            vp.M = 0;
            vp.Phi = Vec3.Zero;
        }

        // --- 6) Final radiance (beauty + debug buffers) ---
        WriteBeauty(width, height, currentVps, beauty, dbg, iterationIndex);
    }

    // -------------------------------------------------------------

    private static List<VisiblePoint> EyePass(
        int width,
        int height,
        ICamera camera,
        Scene.Scene scene,
        ulong baseSeed,
        int iterationIndex,
        float initialRadius,
        Dictionary<int, VisiblePoint> persistent,
        DebugBufferSet dbg,
        SppmIterationStats stats)
    {
        var result = new List<VisiblePoint>(width * height);

        dbg.Clear(DebugBufferId.VisiblePointMask);

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int pixelIndex = y * width + x;

                var vp = EyePassDebugger.TryCreateVisiblePoint(
                    x, y,
                    width, height,
                    camera, scene,
                    baseSeed, iterationIndex,
                    out bool isLambertian);

                if (!isLambertian)
                {
                    stats.VisiblePointsSkippedNonLambertian++;
                    continue;
                }

                if (vp == null)
                {
                    stats.VisiblePointsMissed++;
                    continue;
                }

                if (persistent.TryGetValue(pixelIndex, out var old))
                {
                    vp.N = old.N;
                    vp.Tau = old.Tau;
                    vp.Radius = old.Radius;
                }
                else
                {
                    vp.Radius = initialRadius;
                    vp.N = 0f;
                    vp.Tau = Vec3.Zero;
                    persistent[pixelIndex] = vp;
                }

                result.Add(vp);
                stats.VisiblePointsCreated++;
                dbg.SetPixel(DebugBufferId.VisiblePointMask, x, y, Vec3.One);
            }

        return result;
    }

    private static void Gather(
        VisiblePointGrid grid,
        List<Photon> photons,
        SppmIterationStats stats)
    {
        foreach (var ph in photons)
        {
            bool hitAny = false;

            foreach (var vp in grid.Query(ph.Position))
            {
                var d = vp.Position - ph.Position;
                if (d.LengthSquared() > vp.Radius * vp.Radius)
                    continue;

                Vec3 f = vp.Material.Albedo * MathUtil.InvPi;
                vp.M++;
                vp.Phi += Vec3.Hadamard(ph.Flux, f);
                hitAny = true;
            }

            if (hitAny) stats.PhotonDeposits++;
            else stats.PhotonMisses++;
        }
    }

    private static void WriteBeauty(
        int width,
        int height,
        List<VisiblePoint> vps,
        AccumulationBuffer beauty,
        DebugBufferSet dbg,
        int eyePassCount)
    {
        dbg.Clear(DebugBufferId.PhotonCountN);
        dbg.Clear(DebugBufferId.IndirectPhoton);
        dbg.Clear(DebugBufferId.Radius);

        foreach (var vp in vps)
        {
            int idx = vp.PixelY * width + vp.PixelX;

            if (vp.N <= 0f)
                continue;

            Vec3 indirect =
                vp.Tau / (MathUtil.Pi * vp.Radius * vp.Radius * eyePassCount);

            Vec3 L = Vec3.Hadamard(vp.Beta, indirect);

            beauty.AddSample(vp.PixelX, vp.PixelY, L);

            dbg.Get(DebugBufferId.IndirectPhoton)[idx] = indirect;
            dbg.Get(DebugBufferId.PhotonCountN)[idx] = new Vec3(vp.N);
            dbg.Get(DebugBufferId.Radius)[idx] = new Vec3(vp.Radius);
        }
    }
}