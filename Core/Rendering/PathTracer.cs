using Core.Camera;
using Core.Math;
using Core.Random;
using Core.Sampling;
using Core.Scene;

namespace Core.Rendering;

/// <summary>
/// Path tracer with emissive materials (Milestone 3).
/// No explicit light sampling or MIS yet.
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
        IHittable world,
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

                    sum += Li(ray, world, sampler, maxDepth);
                }

                film[y * width + x] = sum / samplesPerPixel;
            }

        return film;
    }

    private static Vec3 Li(in Ray r, IHittable world, Sampler sampler, int depth)
    {
        if (depth <= 0)
            return Vec3.Zero;

        if (world.Hit(r, 0.001f, float.PositiveInfinity, out var hit))
        {
            Vec3 emitted = hit.Material.Emitted(r, hit);

            if (!hit.Material.Scatter(r, hit, sampler, out var scattered, out var attenuation))
                return emitted;

            Vec3 incoming = Li(scattered, world, sampler, depth - 1);
            return emitted + Vec3.Hadamard(attenuation, incoming);
        }

        return Vec3.Zero;
    }
}