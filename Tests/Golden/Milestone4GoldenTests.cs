

using Core.Camera;
using Core.Lights;
using Core.Materials;
using Core.Math;
using Core.Rendering;
using Core.Scene;
using Tests.Golden;

namespace Tests.Golden;

public class Milestone4GoldenTests
{
    [Fact]
    public void Cornell_NeeMis_Tiny_MatchesGolden()
    {
        const int width = 48;
        const int height = 48;
        const int spp = 16;
        const int maxDepth = 10;
        const ulong seed = 999;

        var red = new Lambertian(new Vec3(0.65f, 0.05f, 0.05f));
        var green = new Lambertian(new Vec3(0.12f, 0.45f, 0.15f));
        var white = new Lambertian(new Vec3(0.73f, 0.73f, 0.73f));
        var lightMat = new DiffuseLight(new Vec3(15f, 15f, 15f));

        var world = new HittableList();
        world.Add(new YZRect(0, 555, 0, 555, 555, green));
        world.Add(new YZRect(0, 555, 0, 555, 0, red));
        world.Add(new XZRect(0, 555, 0, 555, 0, white));
        world.Add(new XZRect(0, 555, 0, 555, 555, white));
        world.Add(new XYRect(0, 555, 0, 555, 555, white));
        world.Add(new FlipFace(new XZRect(213, 343, 227, 332, 554, lightMat)));
        world.Add(new Box(new Vec3(130, 0, 65), new Vec3(295, 165, 230), white));
        world.Add(new Box(new Vec3(265, 0, 295), new Vec3(430, 330, 460), white));

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
            goldenPath: Path.Combine("Golden", "milestone4_cornell_tiny.ptgi"),
            width: width,
            height: height,
            actual: actual,
            rmseThreshold: rmseThreshold);
    }
}
