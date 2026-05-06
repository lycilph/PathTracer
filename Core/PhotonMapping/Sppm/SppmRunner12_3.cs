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

        ComputeRadiusStats(currentVps, stats);

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

                // --- Trace eye ray and find visible point info ---
                var tempVp = EyePassDebugger.TryCreateVisiblePoint(
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

                if (tempVp == null)
                {
                    stats.VisiblePointsMissed++;
                    continue;
                }

                // --- Get or create the persistent visible point ---
                if (!persistent.TryGetValue(pixelIndex, out var vp))
                {
                    vp = new VisiblePoint
                    {
                        PixelX = x,
                        PixelY = y,
                        Radius = initialRadius,
                        N = 0f,
                        Tau = Vec3.Zero
                    };
                    persistent[pixelIndex] = vp;
                }

                // --- Update per-iteration geometric data ---
                vp.Position = tempVp.Position;
                vp.Normal = tempVp.Normal;
                vp.Beta = tempVp.Beta;
                vp.Material = tempVp.Material;

                // --- Reset iteration-local accumulators ---
                vp.M = 0;
                vp.Phi = Vec3.Zero;

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

            beauty.SetPixel(vp.PixelX, vp.PixelY, L);

            dbg.Get(DebugBufferId.IndirectPhoton)[idx] = indirect;
            dbg.Get(DebugBufferId.PhotonCountN)[idx] = new Vec3(vp.N);
            dbg.Get(DebugBufferId.Radius)[idx] = new Vec3(vp.Radius);
        }
    }

    private static void ComputeRadiusStats(
        List<VisiblePoint> vps,
        SppmIterationStats stats)
    {
        if (vps.Count == 0)
        {
            stats.RadiusMin = 0f;
            stats.RadiusAvg = 0f;
            stats.RadiusMax = 0f;
            return;
        }

        float min = float.PositiveInfinity;
        float max = 0f;
        double sum = 0.0;

        for (int i = 0; i < vps.Count; i++)
        {
            float r = vps[i].Radius;
            min = MathF.Min(min, r);
            max = MathF.Max(max, r);
            sum += r;
        }

        stats.RadiusMin = min;
        stats.RadiusMax = max;
        stats.RadiusAvg = (float)(sum / vps.Count);
    }
}