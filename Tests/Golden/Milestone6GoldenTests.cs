using Core.Camera;
using Core.Lights;
using Core.Materials;
using Core.Math;
using Core.Rendering;
using Core.Scene;

namespace Tests.Golden;

public class Milestone6GoldenTests
{
    [Fact]
    public void Cornell_MirrorGlass_Tiny_MatchesGolden()
    {
        const int width = 64;
        const int height = 64;
        const int spp = 32;
        const int maxDepth = 12;
        const ulong seed = 777;

        var red = new Lambertian(new Vec3(0.65f, 0.05f, 0.05f));
        var green = new Lambertian(new Vec3(0.12f, 0.45f, 0.15f));
        var white = new Lambertian(new Vec3(0.73f, 0.73f, 0.73f));
        var lightMat = new DiffuseLight(new Vec3(15f, 15f, 15f));

        var mirror = new Mirror(new Vec3(0.95f, 0.95f, 0.95f));
        var glass = new Dielectric(1.5f);

        var list = new HittableList();
        list.Add(new YZRect(0, 555, 0, 555, 555, green));
        list.Add(new YZRect(0, 555, 0, 555, 0, red));
        list.Add(new XZRect(0, 555, 0, 555, 0, white));
        list.Add(new XZRect(0, 555, 0, 555, 555, white));
        list.Add(new XYRect(0, 555, 0, 555, 555, white));
        list.Add(new FlipFace(new XZRect(213, 343, 227, 332, 554, lightMat)));

        list.Add(new Sphere(new Vec3(190f, 90f, 190f), 90f, mirror));
        list.Add(new Sphere(new Vec3(370f, 90f, 370f), 90f, glass));

        var world = new BvhNode(list.Objects);

        var lights = new List<ILight>
        {
            new RectAreaLightXZ(213, 343, 227, 332, 554, normal: -Vec3.UnitY, radiance: new Vec3(15f,15f,15f))
        };
        var scene = new Core.Scene.Scene(world, lights);

        var camera = new PinholeCamera(
            vfovDegrees: 40f,
            aspectRatio: 1f,
            lookFrom: new Vec3(278f, 278f, -800f),
            lookAt: new Vec3(278f, 278f, 0f),
            vUp: Vec3.UnitY);

        var actual = PathTracer.Render(width, height, spp, maxDepth, camera, scene, baseSeed: seed);

        const float rmseThreshold = 1e-7f;
        GoldenImageAssert.Matches(
            goldenPath: Path.Combine("Golden", "milestone6_cornell_mirror_glass_tiny.ptgi"),
            width: width,
            height: height,
            actual: actual,
            rmseThreshold: rmseThreshold);
    }
}
