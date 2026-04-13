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
        int width = 32;
        int height = 32;
        int spp = 4;

        var world = new HittableList();
        world.Add(new Sphere(new Vec3(0, 0, -1), 0.5f));
        world.Add(new Sphere(new Vec3(0, -100.5f, -1), 100f));

        var camera = new PinholeCamera(
            vfovDegrees: 60f,
            aspectRatio: 1f,
            lookFrom: new Vec3(0, 0, 1),
            lookAt: new Vec3(0, 0, -1),
            vUp: Vec3.UnitY);

        var material = new Lambertian(new Vec3(0.8f));

        var img1 = PathTracer.Render(width, height, spp, camera, world, material, baseSeed: 123);
        var img2 = PathTracer.Render(width, height, spp, camera, world, material, baseSeed: 123);

        Assert.Equal(img1.Length, img2.Length);

        for (int i = 0; i < img1.Length; i++)
        {
            Assert.Equal(img1[i], img2[i]);
        }
    }
}