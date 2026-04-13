using Core.Camera;
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

        // Materials
        var gray = new Lambertian(new Vec3(0.8f, 0.8f, 0.8f));

        // Scene: sphere + ground sphere (now requires materials)
        var world = new HittableList();
        world.Add(new Sphere(new Vec3(0f, 0f, -1f), 0.5f, gray));
        world.Add(new Sphere(new Vec3(0f, -100.5f, -1f), 100f, gray));

        // Camera
        var camera = new PinholeCamera(
            vfovDegrees: 60f,
            aspectRatio: 1f,
            lookFrom: new Vec3(0f, 0.5f, 1.5f),
            lookAt: new Vec3(0f, 0f, -1f),
            vUp: Vec3.UnitY);

        // New signature: includes maxDepth, no default material
        var actual = PathTracer.Render(width, height, spp, maxDepth, camera, world, baseSeed: seed);

        // NOTE:
        // Since Milestone 3 changed the integrator (emission support, black background),
        // your old Milestone 2 golden will NOT match.
        // Run once with UPDATE_GOLDENS=1 to regenerate intentionally.
        const float rmseThreshold = 1e-7f;

        GoldenImageAssert.Matches(
            goldenPath: Path.Combine("Golden", "milestone2_tiny.ptgi"),
            width: width,
            height: height,
            actual: actual,
            rmseThreshold: rmseThreshold);
    }
}