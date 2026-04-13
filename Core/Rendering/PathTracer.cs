using Core.Camera;
using Core.Materials;
using Core.Math;
using Core.Random;
using Core.Sampling;
using Core.Scene;

namespace Core.Rendering;

/// <summary>
/// Diffuse-only Monte Carlo path tracer (Milestone 2).
/// Unbiased estimator of the rendering equation without explicit light sampling.
/// </summary>
public static class PathTracer
{
    public static Vec3[] Render(
        int width,
        int height,
        int samplesPerPixel,
        PinholeCamera camera,
        IHittable world,
        Lambertian defaultMaterial,
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

                    // Jittered pixel sampling for antialiasing.
                    float u = (x + sampler.Next1D()) / width;
                    float v = (y + sampler.Next1D()) / height;
                    var ray = camera.GetRay(u, 1f - v);

                    sum += Li(ray, world, defaultMaterial, sampler, depth: 5);
                }
                film[y * width + x] = sum / samplesPerPixel;
            }
        return film;
    }

    private static Vec3 Li(in Ray r, IHittable world, Lambertian mat, Sampler sampler, int depth)
    {
        if (depth <= 0)
            return Vec3.Zero;

        if (world.Hit(r, 0.001f, float.PositiveInfinity, out var hit))
        {
            // Sample the BSDF to get a new direction wi and a PDF.
            Vec3 wi;
            float pdf;
            Vec3 f = mat.Sample(hit.Normal, sampler, out wi, out pdf);
            if (pdf <= 0f) return Vec3.Zero;

            var scattered = new Ray(hit.Point, wi, r.Time);
            float cos = Vec3.Dot(wi, hit.Normal);

            // Monte Carlo estimator: f * Li * cos / pdf
            Vec3 throughput = f * (cos / pdf);
            Vec3 incoming = Li(scattered, world, mat, sampler, depth - 1);
            return Vec3.Hadamard(throughput, incoming);
        }

        // Environment: simple sky
        Vec3 d = r.Direction.Normalized();
        float t = 0.5f * (d.Y + 1f);
        return (1f - t) * new Vec3(1f, 1f, 1f) + t * new Vec3(0.5f, 0.7f, 1f);
    }
}