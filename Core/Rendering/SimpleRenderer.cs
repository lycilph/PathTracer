using Core.Camera;
using Core.Math;
using Core.Random;
using Core.Sampling;
using Core.Scene;

namespace Core.Rendering;

/// <summary>
/// Milestone 1 renderer: single-sample per pixel, deterministic, no Monte Carlo.
/// Produces a normal-visualization hit shader and a sky gradient background.
/// </summary>
public static class SimpleRenderer
{
    public static Vec3[] Render(int width, int height, PinholeCamera camera, IHittable world)
    {
        var pixels = new Vec3[width * height];
        var rng = new Pcg32(123);
        var sampler = new Sampler(rng);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Deterministic sample at pixel center
                float u = (x + 0.5f) / width;
                float v = (y + 0.5f) / height;

                var ray = camera.GetRay(u, 1f - v, sampler); // flip so y=0 is top row
                pixels[y * width + x] = RayColor(ray, world);
            }
        }

        return pixels;
    }

    private static Vec3 RayColor(in Ray r, IHittable world)
    {
        if (world.Hit(r, 0.001f, float.PositiveInfinity, out var hit))
        {
            // Visualize normal: map [-1,1] -> [0,1]
            return 0.5f * (hit.Normal + Vec3.One);
        }

        // Sky gradient
        Vec3 unitDir = r.Direction.Normalized();
        float t = 0.5f * (unitDir.Y + 1f);
        return (1f - t) * new Vec3(1f, 1f, 1f) + t * new Vec3(0.5f, 0.7f, 1f);
    }
}
