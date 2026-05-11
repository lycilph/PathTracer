using System.Diagnostics;
using Core.Camera;
using Core.Debugging;
using Core.Materials;
using Core.Math;
using Core.Random;
using Core.Rendering;
using Core.Sampling;
using Core.Scene;

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
        Vec3[] fallbackSum,
        int[] fallbackCount,
        ulong baseSeed,
        int iterationIndex,
        int eyePassCount,
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
            baseSeed, iterationIndex, eyePassCount,
            initialRadius,
            persistentVps,
            fallbackSum, fallbackCount,
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
        WriteBeauty(width, height, currentVps, beauty, dbg, eyePassCount, fallbackSum, fallbackCount);
    }

    // -------------------------------------------------------------

    private static List<VisiblePoint> EyePass(
        int width,
        int height,
        ICamera camera,
        Scene.Scene scene,
        ulong baseSeed,
        int iterationIndex,
        int eyePassCount,
        float initialRadius,
        Dictionary<int, VisiblePoint> persistent,
        Vec3[] fallbackSum,
        int[] fallbackCount,
        DebugBufferSet dbg,
        SppmIterationStats stats)
    {
        var result = new List<VisiblePoint>(width * height);

        dbg.Clear(DebugBufferId.VisiblePointMask);

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                int pixelIndex = y * width + x;

                // Build deterministic primary ray + sampler for THIS pixel/iteration
                ulong seed = SeedHash.PixelSampleSeed(x, y, iterationIndex, baseSeed);
                var rng = new Pcg32(seed);
                var sampler = new Sampler(rng);

                // Use a separate deterministic sampler stream for fallback so it doesn’t depend on
                // how many random numbers were consumed during the delta-chain traversal.
                ulong fbSeed = SeedHash.Hash64(seed, 0xF00DF00DUL);
                var fbRng = new Pcg32(fbSeed);
                var fbSampler = new Sampler(fbRng);
                Vec3 Lfb = new Vec3();

                float u = (x + sampler.Next1D()) / width;
                float v = (y + sampler.Next1D()) / height;
                var primaryRay = camera.GetRay(u, 1f - v, sampler);

                // Trace through delta chain to first non-delta hit, accumulating beta.
                Vec3 beta = Vec3.One;
                Ray r = primaryRay;

                HitRecord hit = default;
                bool gotNonDeltaHit = false;

                for (int depth = 0; depth < 12; depth++)
                {
                    if (!scene.World.Hit(r, 0.001f, float.PositiveInfinity, out hit))
                    {
                        gotNonDeltaHit = false;
                        break;
                    }

                    gotNonDeltaHit = true;

                    if (hit.Material.IsDelta)
                    {
                        Vec3 wo = (-r.Direction).Normalized();
                        if (!hit.Material.Sample(wo, hit, sampler, out var wi, out var pdf, out var f))
                        {
                            gotNonDeltaHit = false;
                            break;
                        }

                        float absCos = float.Abs(Vec3.Dot(wi, hit.Normal));
                        if (pdf <= 0f || absCos <= 0f)
                        {
                            gotNonDeltaHit = false;
                            break;
                        }

                        beta = Vec3.Hadamard(beta, f) * (absCos / pdf);
                        r = new Ray(hit.Point, wi, r.Time);
                        continue;
                    }

                    // We reached first non-delta surface
                    break;
                }

                if (!gotNonDeltaHit)
                {
                    // Fallback path tracing sample
                    Lfb = PathTracer.EvaluateRay(primaryRay, scene, fbSampler);
                    fallbackSum[pixelIndex] += Lfb;
                    fallbackCount[pixelIndex] += 1;

                    stats.VisiblePointsMissed++;
                    continue;
                }

                // Lambertian visible point path:
                if (hit.Material is Lambertian lam)
                {
                    // Compute direct at the visible point (using the current sampler)
                    Vec3 wo = (-r.Direction).Normalized();
                    Vec3 directAtHit = DirectLighting.EstimateDirect(hit, wo, scene, sampler, r.Time);

                    if (!persistent.TryGetValue(pixelIndex, out var vp))
                    {
                        vp = new VisiblePoint
                        {
                            PixelX = x,
                            PixelY = y,
                            Radius = initialRadius,
                            N = 0f,
                            Tau = Vec3.Zero,
                            DirectSum = Vec3.Zero
                        };
                        persistent[pixelIndex] = vp;
                    }

                    // Update geometry for this iteration
                    vp.Position = hit.Point;
                    vp.Normal = hit.Normal.Normalized();
                    vp.Beta = beta;
                    vp.Material = lam;

                    // Iteration-local reset
                    vp.M = 0;
                    vp.Phi = Vec3.Zero;

                    // Accumulate direct in camera space
                    vp.DirectSum += Vec3.Hadamard(beta, directAtHit);

                    result.Add(vp);
                    stats.VisiblePointsCreated++;
                    dbg.SetPixel(DebugBufferId.VisiblePointMask, x, y, Vec3.One);
                    continue;
                }

                // Non-Lambertian first non-delta hit → fallback path tracing
                stats.VisiblePointsSkippedNonLambertian++;

                // Evaluate full path tracing for this pixel sample
                // (your existing PathTracer.EvaluateRay must be callable)
                Lfb = PathTracer.EvaluateRay(primaryRay, scene, fbSampler);

                fallbackSum[pixelIndex] += Lfb;
                fallbackCount[pixelIndex] += 1;
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


                const float normalThreshold = 0.9f; // ~25 degrees
                if (Vec3.Dot(vp.Normal, ph.SurfaceNormal) < normalThreshold)
                    continue;


                float planeDist = float.Abs(Vec3.Dot(ph.Position - vp.Position, vp.Normal));
                if (planeDist > 0.01f * vp.Radius) // small fraction of radius
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
        int eyePassCount,
        Vec3[] fallbackSum,
        int[] fallbackCount)
    {
        dbg.Clear(DebugBufferId.PhotonCountN);
        dbg.Clear(DebugBufferId.IndirectPhoton);
        dbg.Clear(DebugBufferId.Radius);
        dbg.Clear(DebugBufferId.DirectLighting);

        // 1) Start by writing fallback average everywhere (or black)
        for (int idx = 0; idx < width * height; idx++)
        {
            Vec3 baseColor = fallbackCount[idx] > 0
                ? (fallbackSum[idx] / fallbackCount[idx])
                : Vec3.Zero;

            int x = idx % width;
            int y = idx / width;
            beauty.SetPixel(x, y, baseColor);
        }

        // 2) Overwrite Lambertian VP pixels with SPPM result
        foreach (var vp in vps)
        {
            int idx = vp.PixelY * width + vp.PixelX;

            Vec3 direct = vp.DirectSum / eyePassCount;

            Vec3 indirectAtHit = Vec3.Zero;
            if (vp.N > 0f)
            {
                indirectAtHit =
                    vp.Tau / (MathUtil.Pi * vp.Radius * vp.Radius * eyePassCount);
            }

            Vec3 indirect = Vec3.Hadamard(vp.Beta, indirectAtHit);

            Vec3 L = direct + indirect;

            beauty.SetPixel(vp.PixelX, vp.PixelY, L);

            // Debug buffers
            dbg.Get(DebugBufferId.DirectLighting)[idx] = direct;
            dbg.Get(DebugBufferId.IndirectPhoton)[idx] = indirectAtHit;
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