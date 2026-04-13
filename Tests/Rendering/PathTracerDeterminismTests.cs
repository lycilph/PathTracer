using Core.Camera;
using Core.Materials;
using Core.Math;
using Core.Rendering;
using Core.Scene;

namespace Tests.Rendering;

public class PathTracerDeterminismTests
{
    [Fact]
    public void Render_IsDeterministic_ForFixedSeed()
    {
        const int width = 8;
        const int height = 8;
        const int spp = 4;
        const int maxDepth = 5;
        const ulong seed = 123;

        var gray = new Lambertian(new Vec3(0.8f, 0.8f, 0.8f));

        var world = new HittableList();
        world.Add(new Sphere(new Vec3(0, 0, -1), 0.5f, gray));
        world.Add(new Sphere(new Vec3(0, -100.5f, -1), 100f, gray));

        var camera = new PinholeCamera(
            vfovDegrees: 60f,
            aspectRatio: 1f,
            lookFrom: new Vec3(0, 0, 1),
            lookAt: new Vec3(0, 0, -1),
            vUp: Vec3.UnitY);

        var img1 = PathTracer.Render(width, height, spp, maxDepth, camera, world, baseSeed: seed);
        var img2 = PathTracer.Render(width, height, spp, maxDepth, camera, world, baseSeed: seed);

        Assert.Equal(img1.Length, img2.Length);
        for (int i = 0; i < img1.Length; i++)
            Assert.Equal(img1[i], img2[i]);
    }
}