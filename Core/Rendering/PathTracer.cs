using Core.Camera;
using Core.Lights;
using Core.Math;
using Core.Random;
using Core.Sampling;
using Core.Scene;

namespace Core.Rendering;

/// <summary>
/// Milestone 4 integrator: Next Event Estimation + MIS.
/// Supports emissive area lights via Scene.Lights plus emissive materials on geometry.
/// Background is black.
/// </summary>
public static class PathTracer
{
    public static Vec3[] Render(
        int width,
        int height,
        int samplesPerPixel,
        int maxDepth,
        PinholeCamera camera,
        Scene.Scene scene,
        ulong baseSeed = 1)
    {
        var film = new Vec3[width * height];

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                Vec3 sum = Vec3.Zero;

                for (int s = 0; s < samplesPerPixel; s++)
                {
                    ulong seed = SeedHash.PixelSampleSeed(x, y, s, baseSeed);
                    var rng = new Pcg32(seed);
                    var sampler = new Sampler(rng);

                    float u = (x + sampler.Next1D()) / width;
                    float v = (y + sampler.Next1D()) / height;
                    var ray = camera.GetRay(u, 1f - v);

                    sum += Li(ray, scene, sampler, maxDepth);
                }

                film[y * width + x] = sum / samplesPerPixel;
            }

        return film;
    }

    private static Vec3 Li(in Ray ray, Scene.Scene scene, Sampler sampler, int depth)
    {
        if (depth <= 0)
            return Vec3.Zero;

        if (!scene.World.Hit(ray, 0.001f, float.PositiveInfinity, out var hit))
            return Vec3.Zero;

        var mat = hit.Material;
        Vec3 wo = (-ray.Direction).Normalized();

        // Emission at the surface (if any)
        Vec3 L = mat.Emitted(ray, hit);

        // Direct lighting via Next Event Estimation (skip for delta materials)
        if (!mat.IsDelta && scene.Lights.Count > 0)
        {
            L += EstimateDirect(hit, wo, scene, sampler, ray.Time);
        }

        // Sample BSDF to continue path
        if (depth == 1) return L;

        if (!mat.Sample(wo, hit, sampler, out var wi, out var bsdfPdf, out var f))
            return L;

        float cos = Vec3.Dot(wi, hit.Normal);
        if (bsdfPdf <= 0f || cos <= 0f) return L;

        Vec3 throughput = f * (cos / bsdfPdf);
        var scattered = new Ray(hit.Point, wi, ray.Time);

        // If the BSDF-sampled ray hits a light, apply MIS weight against light sampling PDF.
        if (scene.World.Hit(scattered, 0.001f, float.PositiveInfinity, out var hit2))
        {
            Vec3 Le = hit2.Material.Emitted(scattered, hit2);
            if (!Le.NearZero() && !mat.IsDelta)
            {
                float lightPdf = SceneLightPdf(scene, hit.Point, wi);
                float w = Mis.PowerHeuristic(bsdfPdf, lightPdf);
                L += Vec3.Hadamard(throughput, Le) * w;
                return L;
            }
        }

        // Otherwise continue recursively
        L += Vec3.Hadamard(throughput, Li(scattered, scene, sampler, depth - 1));
        return L;
    }

    private static Vec3 EstimateDirect(in HitRecord hit, in Vec3 wo, Scene.Scene scene, Sampler sampler, float time)
    {
        int nLights = scene.Lights.Count;
        if (nLights == 0) return Vec3.Zero;

        int index = (int)(sampler.Next1D() * nLights);
        if (index == nLights) index = nLights - 1;

        ILight light = scene.Lights[index];
        var ls = light.Sample(hit.Point, sampler);
        if (ls.Pdf <= 0f) return Vec3.Zero;

        float cosSurface = Vec3.Dot(ls.Wi, hit.Normal);
        if (cosSurface <= 0f) return Vec3.Zero;

        // Visibility
        var shadow = new Ray(hit.Point, ls.Wi, time);
        if (scene.World.Hit(shadow, 0.001f, ls.Distance - 0.001f, out _))
            return Vec3.Zero;

        var f = hit.Material.Evaluate(wo, ls.Wi, hit);
        float bsdfPdf = hit.Material.Pdf(wo, ls.Wi, hit);

        // Include light selection probability
        float lightPdf = ls.Pdf / nLights;

        float w = Mis.PowerHeuristic(lightPdf, bsdfPdf);

        // Contribution: Le * f * cos / pdf  (component-wise in RGB)
        return Vec3.Hadamard(f, ls.Radiance) * (cosSurface * w / lightPdf);
    }

    private static float SceneLightPdf(Scene.Scene scene, in Vec3 refPoint, in Vec3 wi)
    {
        int nLights = scene.Lights.Count;
        if (nLights == 0) return 0f;

        float sum = 0f;
        for (int i = 0; i < nLights; i++)
            sum += scene.Lights[i].Pdf(refPoint, wi);

        return sum / nLights;
    }
}
