using Core.Camera;
using Core.Materials;
using Core.Math;
using Core.Rendering;
using Core.Scene;

namespace Tests.Rendering;

public class PathTracerSanityTests
{
    [Fact]
    public void Render_ProducesFiniteNonNegativeValues()
    {
        const int width = 16;
        const int height = 16;
        const int spp = 8;
        const int maxDepth = 5;

        var gray = new Lambertian(new Vec3(0.8f, 0.8f, 0.8f));

        var world = new HittableList();
        world.Add(new Sphere(new Vec3(0f, 0f, -1f), 0.5f, gray));

        var camera = new PinholeCamera(
            vfovDegrees: 60f,
            aspectRatio: 1f,
            lookFrom: new Vec3(0, 0, 1),
            lookAt: new Vec3(0, 0, -1),
            vUp: Vec3.UnitY);

        var img = PathTracer.Render(width, height, spp, maxDepth, camera, world, baseSeed: 1);

        foreach (var c in img)
        {
            Assert.False(float.IsNaN(c.X) || float.IsNaN(c.Y) || float.IsNaN(c.Z));
            Assert.False(float.IsInfinity(c.X) || float.IsInfinity(c.Y) || float.IsInfinity(c.Z));

            // Radiance should not be negative in our current model
            Assert.True(c.X >= 0f && c.Y >= 0f && c.Z >= 0f);
        }
    }
}
