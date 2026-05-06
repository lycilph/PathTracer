using System.Diagnostics;
using Core.Camera;
using Core.Debugging;
using Core.Materials;
using Core.Math;
using Core.Random;
using Core.Sampling;

namespace Core.PhotonMapping.Sppm;

public static class SppmDebugRunner12_2
{
    public static void RunOneIteration(
        int width,
        int height,
        ICamera camera,
        Scene.Scene scene,
        DebugBufferSet dbg,
        ulong baseSeed,
        int iterationIndex,
        int photonsPerPass,
        int photonMaxDepth,
        float radius,
        SppmIterationStats outStats)
    {
        var sw = Stopwatch.StartNew();

        // 1) Eye pass: create visible points and fill g-buffers (reuse your existing EyePassDebugger for AOVs)
        var visiblePoints = new List<VisiblePoint>(width * height);

        dbg.Clear(DebugBufferId.VisiblePointMask);
        dbg.Clear(DebugBufferId.PhotonCountM);
        dbg.Clear(DebugBufferId.IndirectPhoton);

        EyePassToVisiblePoints(width, height, camera, scene, baseSeed, iterationIndex,
            radius, visiblePoints, dbg, outStats);

        outStats.EyePassMs = sw.Elapsed.TotalMilliseconds;

        // 2) Build grid
        var grid = new VisiblePointGrid(cellSize: radius);
        for (int i = 0; i < visiblePoints.Count; i++)
            grid.Insert(visiblePoints[i]);

        // 3) Photon pass (reuse your 12.1 tracer)
        sw.Restart();
        var pstats = new PhotonTraceStats();
        var photons = PhotonTracer.TracePhotonPass(
            scene: scene,
            photonsPerPass: photonsPerPass,
            maxDepth: photonMaxDepth,
            baseSeed: baseSeed,
            iterationIndex: iterationIndex,
            stats: pstats);

        outStats.PhotonsStored = pstats.PhotonsStored;
        outStats.PhotonPassMs = sw.Elapsed.TotalMilliseconds;

        // 4) Gather: deposit photons into nearby visible points
        sw.Restart();
        GatherPhotons(grid, photons, outStats);
        outStats.GatherMs = sw.Elapsed.TotalMilliseconds;

        // 5) Fill debug buffers from visible points
        BuildDebugBuffersFromVisiblePoints(width, height, visiblePoints, dbg);
    }

    private static void EyePassToVisiblePoints(
        int width,
        int height,
        ICamera camera,
        Scene.Scene scene,
        ulong baseSeed,
        int iterationIndex,
        float radius,
        List<VisiblePoint> outVps,
        DebugBufferSet dbg,
        SppmIterationStats stats)
    {
        // One camera path per pixel, deterministic per (x,y,iteration)
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                ulong seed = SeedHash.PixelSampleSeed(x, y, iterationIndex, baseSeed);
                var rng = new Pcg32(seed);
                var sampler = new Sampler(rng);

                float u = (x + sampler.Next1D()) / width;
                float v = (y + sampler.Next1D()) / height;

                var ray = camera.GetRay(u, 1f - v, sampler);

                // Trace until first non-delta surface (like path tracing), accumulating throughput
                Vec3 beta = Vec3.One;
                Ray r = ray;

                for (int depth = 0; depth < 12; depth++)
                {
                    if (!scene.World.Hit(r, 0.001f, float.PositiveInfinity, out var hit))
                    {
                        stats.VisiblePointsMissed++;
                        break;
                    }

                    // If hit is delta, continue tracing (specular chain)
                    if (hit.Material.IsDelta)
                    {
                        Vec3 wo = (-r.Direction).Normalized();

                        if (!hit.Material.Sample(wo, hit, sampler, out var wi, out var pdf, out var f))
                            break;

                        float cos = Vec3.Dot(wi, hit.Normal);
                        float absCos = float.Abs(cos);
                        if (pdf <= 0f || absCos <= 0f) break;

                        beta = Vec3.Hadamard(beta, f) * (absCos / pdf);
                        r = new Ray(hit.Point, wi, r.Time);
                        continue;
                    }

                    // Non-delta: for 12.2 we only accept Lambertian as visible points
                    if (hit.Material is Lambertian lam)
                    {
                        var vp = new VisiblePoint
                        {
                            PixelX = x,
                            PixelY = y,
                            Position = hit.Point,
                            Normal = hit.Normal.Normalized(),
                            Beta = beta,
                            Material = lam,
                            Radius = radius
                        };

                        outVps.Add(vp);
                        stats.VisiblePointsCreated++;

                        dbg.SetPixel(DebugBufferId.VisiblePointMask, x, y, Vec3.One);
                    }
                    else
                    {
                        stats.VisiblePointsSkippedNonLambertian++;
                    }

                    break;
                }
            }
    }

    private static void GatherPhotons(VisiblePointGrid grid, List<Photon> photons, SppmIterationStats stats)
    {
        for (int i = 0; i < photons.Count; i++)
        {
            var ph = photons[i];
            bool deposited = false;

            foreach (var vp in grid.Query(ph.Position))
            {
                // Distance check
                var d = vp.Position - ph.Position;
                float r2 = vp.Radius * vp.Radius;
                if (d.LengthSquared() > r2)
                    continue;

                // Contribution:
                // In 12.2 we compute a simple flux*albedo contribution for debug.
                // In 12.3 we’ll incorporate the proper SPPM τ update and BRDF term.
                // Lambertian f = albedo / pi
                Vec3 f = vp.Material.Albedo * MathUtil.InvPi;

                vp.M++;
                vp.Phi += Vec3.Hadamard(ph.Flux, f);

                deposited = true;
            }

            if (deposited) stats.PhotonDeposits++;
            else stats.PhotonMisses++;
        }
    }

    private static void BuildDebugBuffersFromVisiblePoints(
        int width,
        int height,
        List<VisiblePoint> vps,
        DebugBufferSet dbg)
    {
        // Clear per-pixel outputs
        dbg.Clear(DebugBufferId.PhotonCountM);
        dbg.Clear(DebugBufferId.IndirectPhoton);

        var mBuf = dbg.Get(DebugBufferId.PhotonCountM);
        var iBuf = dbg.Get(DebugBufferId.IndirectPhoton);

        // Put M and a provisional indirect estimate into per-pixel buffers
        for (int i = 0; i < vps.Count; i++)
        {
            var vp = vps[i];
            int idx = vp.PixelY * width + vp.PixelX;

            // M heatmap in grayscale
            float m = vp.M;
            mBuf[idx] = new Vec3(m, m, m);

            // Provisional indirect photon contribution:
            // Li ≈ Phi / (pi * R^2 * Ne)  (we don’t have Ne normalization here yet)
            // For 12.2 we just visualize Phi magnitude scaled.
            Vec3 phi = vp.Phi;
            iBuf[idx] = phi;
        }

        // Normalize for display friendliness (log scale)
        NormalizeLog(mBuf);
        NormalizeLog(iBuf);
    }

    private static void NormalizeLog(Vec3[] buf)
    {
        float max = 0f;
        for (int i = 0; i < buf.Length; i++)
            if (buf[i].X > max) max = buf[i].X;

        if (max <= 0f) return;

        float inv = 1f / max;

        for (int i = 0; i < buf.Length; i++)
        {
            float v = buf[i].X * inv;
            v = float.Log(1f + 20f * v) / float.Log(21f);
            buf[i] = new Vec3(v, v, v);
        }
    }
}