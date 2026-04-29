using Core.Camera;
using Core.Lights;
using Core.Materials;
using Core.Math;
using Core.Rendering;
using Core.Scene;

namespace Tests.Golden;

public class Milestone7GoldenTests
{
    [Fact]
    public void Microfacet_Showcase_Tiny_MatchesGolden()
    {
        const int width = 64;
        const int height = 64;
        const int spp = 32;
        const int maxDepth = 10;
        const ulong seed = 7007;

        var ground = new Lambertian(new Vec3(0.75f, 0.75f, 0.75f));
        var lightMat = new DiffuseLight(new Vec3(20f, 20f, 20f));

        var metalRough = new MicrofacetMetal(new Vec3(0.95f, 0.64f, 0.54f), roughness: 0.6f);
        var metalMid = new MicrofacetMetal(new Vec3(0.95f, 0.64f, 0.54f), roughness: 0.25f);
        var metalSharp = new MicrofacetMetal(new Vec3(0.95f, 0.64f, 0.54f), roughness: 0.05f);

        var list = new HittableList();
        list.Add(new XZRect(-10, 10, -10, 10, k: 0, ground));
        list.Add(new FlipFace(new XZRect(-2, 2, -2, 2, k: 5f, lightMat)));

        list.Add(new Sphere(new Vec3(-1.5f, 1f, -3f), 1f, metalRough));
        list.Add(new Sphere(new Vec3(0.0f, 1f, -3f), 1f, metalMid));
        list.Add(new Sphere(new Vec3(1.5f, 1f, -3f), 1f, metalSharp));

        var world = new BvhNode(list.Objects);

        var lights = new List<ILight>
        {
            new RectAreaLightXZ(-2, 2, -2, 2, k: 5f, normal: -Vec3.UnitY, radiance: new Vec3(20f,20f,20f))
        };
        var scene = new Core.Scene.Scene(world, lights);

        var camera = new PinholeCamera(
            vfovDegrees: 40f,
            aspectRatio: 1f,
            lookFrom: new Vec3(0f, 2f, 3f),
            lookAt: new Vec3(0f, 1f, -3f),
            vUp: Vec3.UnitY);

        var actual = PathTracer.Render(width, height, spp, maxDepth, camera, scene, baseSeed: seed);

        const float rmseThreshold = 1e-7f;
        GoldenImageAssert.Matches(
            goldenPath: Path.Combine("Golden", "milestone7_microfacet_tiny.ptgi"),
            width: width,
            height: height,
            actual: actual,
            rmseThreshold: rmseThreshold);
    }
}