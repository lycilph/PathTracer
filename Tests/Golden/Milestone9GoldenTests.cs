using Core.Camera;
using Core.Lights;
using Core.Materials;
using Core.Math;
using Core.Rendering;
using Core.Scene;

namespace Tests.Golden;

public class Milestone9GoldenTests
{
    [Fact]
    public void MotionBlur_MovingSphere_Tiny_MatchesGolden()
    {
        const int width = 64;
        const int height = 64;
        const int spp = 32;
        const int maxDepth = 8;
        const ulong seed = 9009;

        var white = new Lambertian(new Vec3(0.75f, 0.75f, 0.75f));
        var red = new Lambertian(new Vec3(0.85f, 0.25f, 0.25f));
        var lightMat = new DiffuseLight(new Vec3(35f, 35f, 35f));

        var list = new HittableList();
        list.Add(new XZRect(-20, 20, -30, 10, k: 0f, white));
        list.Add(new FlipFace(new XZRect(-2.5f, 2.5f, -10f, -5f, k: 10f, lightMat)));

        list.Add(new MovingSphere(
            center0: new Vec3(-3f, 1.0f, -8f),
            center1: new Vec3(3f, 1.0f, -8f),
            time0: 0f,
            time1: 1f,
            radius: 1.0f,
            material: red));

        var world = new BvhNode(list.Objects);

        var lights = new List<ILight>
        {
            new RectAreaLightXZ(-2.5f, 2.5f, -10f, -5f, k: 10f, normal: -Vec3.UnitY, radiance: new Vec3(35f,35f,35f))
        };
        var scene = new Core.Scene.Scene(world, lights);

        var lookFrom = new Vec3(0f, 3f, 6f);
        var lookAt = new Vec3(0f, 1f, -8f);

        ICamera camera = new PinholeCamera(
            vfovDegrees: 35f,
            aspectRatio: 1f,
            lookFrom: lookFrom,
            lookAt: lookAt,
            vUp: Vec3.UnitY,
            shutterOpen: 0f,
            shutterClose: 1f);

        var actual = PathTracer.Render(width, height, spp, maxDepth, camera, scene, baseSeed: seed);

        const float rmseThreshold = 1e-7f;
        GoldenImageAssert.Matches(
            goldenPath: Path.Combine("Golden", "milestone9_motion_blur_tiny.ptgi"),
            width: width,
            height: height,
            actual: actual,
            rmseThreshold: rmseThreshold);
    }
}