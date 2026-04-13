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
        int width = 16;
        int height = 16;
        int spp = 8;

        var world = new HittableList();
        world.Add(new Sphere(new Vec3(0, 0, -1), 0.5f));

        var camera = new PinholeCamera(
            vfovDegrees: 60f,
            aspectRatio: 1f,
            lookFrom: new Vec3(0, 0, 1),
            lookAt: new Vec3(0, 0, -1),
            vUp: Vec3.UnitY);

        var material = new Lambertian(new Vec3(0.8f));

        var img = PathTracer.Render(width, height, spp, camera, world, material);

        foreach (var c in img)
        {
            Assert.False(float.IsNaN(c.X));
            Assert.False(float.IsNaN(c.Y));
            Assert.False(float.IsNaN(c.Z));

            Assert.False(float.IsInfinity(c.X));
            Assert.False(float.IsInfinity(c.Y));
            Assert.False(float.IsInfinity(c.Z));

            Assert.True(c.X >= 0f);
            Assert.True(c.Y >= 0f);
            Assert.True(c.Z >= 0f);
        }
    }
}