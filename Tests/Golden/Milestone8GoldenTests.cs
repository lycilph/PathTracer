using Core.Camera;
using Core.Lights;
using Core.Materials;
using Core.Math;
using Core.Rendering;
using Core.Scene;

namespace Tests.Golden;

public class Milestone8GoldenTests
{
    [Fact]
    public void ThinLensDoF_Tiny_MatchesGolden()
    {
        const int width = 64;
        const int height = 64;
        const int spp = 32;
        const int maxDepth = 8;
        const ulong seed = 8008;

        // Materials
        var gray = new Lambertian(new Vec3(0.7f, 0.7f, 0.7f));
        var red = new Lambertian(new Vec3(0.8f, 0.2f, 0.2f));
        var green = new Lambertian(new Vec3(0.2f, 0.8f, 0.2f));
        var blue = new Lambertian(new Vec3(0.2f, 0.2f, 0.8f));
        var lightMat = new DiffuseLight(new Vec3(25f, 25f, 25f));

        var list = new HittableList();

        // Ground
        list.Add(new XZRect(-10, 10, -10, 10, k: 0, gray));

        // Light above
        list.Add(new FlipFace(new XZRect(-2, 2, -2, 2, k: 6f, lightMat)));

        // Three spheres at different distances
        list.Add(new Sphere(new Vec3(-1.2f, 1f, -2.5f), 1f, red));   // near
        list.Add(new Sphere(new Vec3(0.0f, 1f, -4.0f), 1f, green)); // focus target
        list.Add(new Sphere(new Vec3(1.2f, 1f, -6.0f), 1f, blue));  // far

        var world = new BvhNode(list.Objects);

        var lights = new List<ILight>
        {
            new RectAreaLightXZ(-2, 2, -2, 2, k: 6f, normal: -Vec3.UnitY, radiance: new Vec3(25f,25f,25f))
        };
        var scene = new Core.Scene.Scene(world, lights);

        // Camera with DoF
        var lookFrom = new Vec3(0f, 2f, 3f);
        var lookAt = new Vec3(0f, 1f, -4f); // aim at the middle sphere
        float focusDist = (lookAt - lookFrom).Length(); // focus at lookAt distance
        float apertureRadius = 0.08f;

        ICamera camera = new ThinLensCamera(
            vfovDegrees: 35f,
            aspectRatio: 1f,
            lookFrom: lookFrom,
            lookAt: lookAt,
            vUp: Vec3.UnitY,
            focusDistance: focusDist,
            apertureRadius: apertureRadius);

        var actual = PathTracer.Render(width, height, spp, maxDepth, camera, scene, baseSeed: seed);

        const float rmseThreshold = 1e-7f;
        GoldenImageAssert.Matches(
            goldenPath: Path.Combine("Golden", "milestone8_thinlens_dof_tiny.ptgi"),
            width: width,
            height: height,
            actual: actual,
            rmseThreshold: rmseThreshold);
    }
}