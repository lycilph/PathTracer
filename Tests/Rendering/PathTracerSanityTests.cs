using Core.Camera;
using Core.Lights;
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
        var lightMat = new DiffuseLight(new Vec3(5f, 5f, 5f));

        var world = new HittableList();
        world.Add(new Sphere(new Vec3(0f, 0f, -1f), 0.5f, gray));
        // Add a small light in front (sphere emissive) - still no light sampling here, but emission should be finite
        world.Add(new Sphere(new Vec3(0f, 2f, -1f), 0.25f, lightMat));

        var scene = new Core.Scene.Scene(world, new List<ILight>());

        var camera = new PinholeCamera(60f, 1f, new Vec3(0, 0, 1), new Vec3(0, 0, -1), Vec3.UnitY);

        var img = PathTracer.Render(width, height, spp, maxDepth, camera, scene, baseSeed: 1);

        foreach (var c in img)
        {
            Assert.False(float.IsNaN(c.X) || float.IsNaN(c.Y) || float.IsNaN(c.Z));
            Assert.False(float.IsInfinity(c.X) || float.IsInfinity(c.Y) || float.IsInfinity(c.Z));
            Assert.True(c.X >= 0f && c.Y >= 0f && c.Z >= 0f);
        }
    }
}