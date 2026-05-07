using Core.Camera;
using Core.Materials;
using Core.Math;
using Core.PhotonMapping.Sppm;
using Core.Random;
using Core.Rendering;
using Core.Sampling;

namespace Core.Debugging;

public static class EyePassDebugger
{
    public static void Fill(
        int width,
        int height,
        ICamera camera,
        Scene.Scene scene,
        DebugBufferSet dbg,
        ulong baseSeed,
        int iterationIndex)
    {
        // One sample per pixel for debug buffers.
        // Deterministic per pixel+iteration so we can compare runs.
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                ulong seed = SeedHash.PixelSampleSeed(x, y, iterationIndex, baseSeed);
                var rng = new Pcg32(seed);
                var sampler = new Sampler(rng);

                float u = (x + sampler.Next1D()) / width;
                float v = (y + sampler.Next1D()) / height;

                var ray = camera.GetRay(u, 1f - v, sampler);

                if (!scene.World.Hit(ray, 0.001f, float.PositiveInfinity, out var hit))
                {
                    dbg.SetPixel(DebugBufferId.Depth, x, y, Vec3.Zero);
                    dbg.SetPixel(DebugBufferId.Normal, x, y, Vec3.Zero);
                    dbg.SetPixel(DebugBufferId.Albedo, x, y, Vec3.Zero);
                    dbg.SetPixel(DebugBufferId.VisiblePointMask, x, y, Vec3.Zero);
                    dbg.SetPixel(DebugBufferId.Throughput, x, y, Vec3.Zero);
                    continue;
                }

                // Depth as raw t (we’ll normalize for display in UI)
                dbg.SetPixel(DebugBufferId.Depth, x, y, new Vec3(hit.T, hit.T, hit.T));

                // Normal visualize: map [-1,1] to [0,1]
                Vec3 n = hit.Normal.Normalized();
                dbg.SetPixel(DebugBufferId.Normal, x, y, (n * 0.5f) + (Vec3.One * 0.5f));

                // Albedo (Lambertian only for now)
                Vec3 albedo = hit.Material is Lambertian lam ? lam.Albedo : new Vec3(0.3f, 0.3f, 0.3f);
                dbg.SetPixel(DebugBufferId.Albedo, x, y, albedo);

                // Visible point mask: first non-delta hit and Lambertian only (choice #1)
                bool vp = !hit.Material.IsDelta && hit.Material is Lambertian;
                dbg.SetPixel(DebugBufferId.VisiblePointMask, x, y, vp ? Vec3.One : Vec3.Zero);

                // Throughput magnitude proxy (for debugging)
                float lum = 0.2126f * albedo.X + 0.7152f * albedo.Y + 0.0722f * albedo.Z;
                dbg.SetPixel(DebugBufferId.Throughput, x, y, new Vec3(lum, lum, lum));
            }
        }
    }


    public static VisiblePoint? TryCreateVisiblePoint(
        int x, int y,
        int width, int height,
        ICamera camera,
        Scene.Scene scene,
        ulong baseSeed,
        int iterationIndex,
        out bool isLambertian,
        out Vec3 directAtHit)
    {
        directAtHit = Vec3.Zero;

        ulong seed = SeedHash.PixelSampleSeed(x, y, iterationIndex, baseSeed);
        var rng = new Pcg32(seed);
        var sampler = new Sampler(rng);

        float u = (x + sampler.Next1D()) / width;
        float v = (y + sampler.Next1D()) / height;

        var ray = camera.GetRay(u, 1f - v, sampler);
        Vec3 beta = Vec3.One;
        Ray r = ray;

        for (int depth = 0; depth < 12; depth++)
        {
            if (!scene.World.Hit(r, 0.001f, float.PositiveInfinity, out var hit))
            {
                isLambertian = false;
                return null;
            }

            if (hit.Material.IsDelta)
            {
                Vec3 woDelta = (-r.Direction).Normalized();
                if (!hit.Material.Sample(woDelta, hit, sampler, out var wi, out var pdf, out var f))
                    break;

                float absCos = float.Abs(Vec3.Dot(wi, hit.Normal));
                beta = Vec3.Hadamard(beta, f) * (absCos / pdf);
                r = new Ray(hit.Point, wi, r.Time);
                continue;
            }

            if (hit.Material is Lambertian lam)
            {
                isLambertian = true;

                Vec3 wo = (-r.Direction).Normalized();

                // Direct lighting at the surface point (NEE)
                directAtHit = DirectLighting.EstimateDirect(hit, wo, scene, sampler, r.Time);

                return new VisiblePoint
                {
                    PixelX = x,
                    PixelY = y,
                    Position = hit.Point,
                    Normal = hit.Normal.Normalized(),
                    Beta = beta,
                    Material = lam
                };
            }

            isLambertian = false;
            return null;
        }

        isLambertian = false;
        return null;
    }
}