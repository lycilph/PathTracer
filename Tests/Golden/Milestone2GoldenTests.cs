using Core.Camera;
using Core.Lights;
using Core.Materials;
using Core.Math;
using Core.Rendering;
using Core.Scene;

namespace Tests.Golden;

public class Milestone2GoldenTests
{
    [Fact]
    public void DiffusePathTracer_TinyScene_MatchesGolden()
    {
        const int width = 32;
        const int height = 32;
        const int spp = 16;
        const int maxDepth = 5;
        const ulong seed = 123;

        var gray = new Lambertian(new Vec3(0.8f, 0.8f, 0.8f));

        var world = new HittableList();
        world.Add(new Sphere(new Vec3(0f, 0f, -1f), 0.5f, gray));
        world.Add(new Sphere(new Vec3(0f, -100.5f, -1f), 100f, gray));

        var scene = new Core.Scene.Scene(world, new List<ILight>());

        var camera = new PinholeCamera(
            vfovDegrees: 60f,
            aspectRatio: 1f,
            lookFrom: new Vec3(0f, 0.5f, 1.5f),
            lookAt: new Vec3(0f, 0f, -1f),
            vUp: Vec3.UnitY);

        var actual = PathTracer.Render(width, height, spp, maxDepth, camera, scene, baseSeed: seed);

        const float rmseThreshold = 1e-7f;
        GoldenImageAssert.Matches(
            goldenPath: Path.Combine("Golden", "milestone2_tiny.ptgi"),
            width: width,
            height: height,
            actual: actual,
            rmseThreshold: rmseThreshold);
    }
}