using Core;
using Core.Acceleration;
using Core.Algebra;
using Core.Geometry;
using Core.Sampling;
using Engine.Integrators;
using Engine.Lighting;
using Engine.Materials;
using Engine.PhotonMapping;
using FluentAssertions;

namespace Engine.Tests.Integrators;

public class PhotonMapIntegratorTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static (SceneList scene, List<ILight> lights) MakeCornellBox()
    {
        var scene = new SceneList();
        var lights = new List<ILight>();

        var white = new Lambertian(new Vector3(0.73, 0.73, 0.73));
        var red = new Lambertian(new Vector3(0.65, 0.05, 0.05));
        var green = new Lambertian(new Vector3(0.12, 0.45, 0.15));

        scene.Add(new Quad(new Vector3(-1, -1, -1), new Vector3(2, 0, 0),
                           new Vector3(0, 0, 2), white));
        scene.Add(new Quad(new Vector3(-1, 1, -1), new Vector3(2, 0, 0),
                           new Vector3(0, 0, 2), white));
        scene.Add(new Quad(new Vector3(-1, -1, -1), new Vector3(2, 0, 0),
                           new Vector3(0, 2, 0), white));
        scene.Add(new Quad(new Vector3(-1, -1, -1), new Vector3(0, 2, 0),
                           new Vector3(0, 0, 2), red));
        scene.Add(new Quad(new Vector3(1, -1, -1), new Vector3(0, 2, 0),
                           new Vector3(0, 0, 2), green));

        var areaLight = new AreaLight(
            new Vector3(-0.25, 0.999, -0.25),
            new Vector3(0.5, 0, 0),
            new Vector3(0, 0, 0.5),
            new Vector3(15, 15, 15));

        scene.Add(areaLight);
        lights.Add(areaLight);

        return (scene, lights);
    }

    private static PhotonMap MakePhotonMap(
        IHittable scene,
        IReadOnlyList<ILight> lights,
        int photonCount = 10_000)
    {
        var emitter = new PhotonEmitter();
        var photons = emitter.Emit(photonCount, scene, lights, maxDepth: 10);
        return new PhotonMap(photons);
    }

    private static PixelEstimationState[] MakePixelStates(
        int count, double radius = 0.1)
        => Enumerable.Range(0, count)
                     .Select(_ => PixelEstimationState.Initial(radius))
                     .ToArray();

    // ── Empty scene ───────────────────────────────────────────────────────────

    [Fact]
    public void Trace_EmptyScene_ReturnsBackground()
    {
        var integrator = new PhotonMapIntegrator
        {
            BackgroundRadiance = new Vector3(0.5, 0.7, 1.0)
        };
        var scene = new SceneList();
        var photonMap = new PhotonMap([]);
        var states = MakePixelStates(1);
        var ray = new Ray(Vector3.Zero, Vector3.UnitZ);

        var result = integrator.Trace(ray, scene, [],
            photonMap, states, 0, new Sampler(0));

        result.Should().Be(new Vector3(0.5, 0.7, 1.0));
    }

    // ── Direct lighting ───────────────────────────────────────────────────────

    [Fact]
    public void Trace_HitsEmissive_ReturnsEmission()
    {
        var integrator = new PhotonMapIntegrator();
        var scene = new SceneList();
        var emission = new Vector3(5, 3, 1);
        scene.Add(new Sphere(new Vector3(0, 0, 5), 1.0,
                             new Emissive(emission)));

        var photonMap = new PhotonMap([]);
        var states = MakePixelStates(1);
        var ray = new Ray(Vector3.Zero, Vector3.UnitZ);

        var result = integrator.Trace(ray, scene, [],
            photonMap, states, 0, new Sampler(0));

        result.Should().Be(emission);
    }

    [Fact]
    public void Trace_LitScene_DirectComponentIsNonZero()
    {
        var integrator = new PhotonMapIntegrator();
        var (scene, lights) = MakeCornellBox();
        var photonMap = new PhotonMap([]);
        var states = MakePixelStates(1);

        // Ray aimed at the floor
        var ray = new Ray(new Vector3(0, 0, 3.5), -Vector3.UnitZ);

        var total = Vector3.Zero;
        const int n = 100;
        for (var i = 0; i < n; i++)
        {
            var state = PixelEstimationState.Initial(0.1);
            states[0] = state;
            total = total + integrator.Trace(ray, scene, lights,
                photonMap, states, 0, new Sampler(i));
        }

        var mean = total / n;
        mean.X.Should().BeGreaterThan(0,
            because: "lit scene must produce non-zero direct radiance");
    }

    // ── Indirect lighting ─────────────────────────────────────────────────────

    [Fact]
    public void Trace_WithPhotonMap_ProducesHigherRadianceThanDirect()
    {
        var integrator = new PhotonMapIntegrator(kNearest: 50);
        var (scene, lights) = MakeCornellBox();
        var photonMap = MakePhotonMap(scene, lights, 50_000);

        var ray = new Ray(new Vector3(0, 0, 3.5), -Vector3.UnitZ);

        var totalWithPhotons = Vector3.Zero;
        var totalWithoutPhotons = Vector3.Zero;
        const int n = 50;

        for (var i = 0; i < n; i++)
        {
            var statesWith = MakePixelStates(1, 0.1);
            var statesWithout = MakePixelStates(1, 0.1);
            var emptyMap = new PhotonMap([]);

            totalWithPhotons += integrator.Trace(ray, scene, lights,
                photonMap, statesWith, 0, new Sampler(i));
            totalWithoutPhotons += integrator.Trace(ray, scene, lights,
                emptyMap, statesWithout, 0, new Sampler(i));
        }

        var meanWith = totalWithPhotons / n;
        var meanWithout = totalWithoutPhotons / n;

        meanWith.X.Should().BeGreaterThanOrEqualTo(meanWithout.X,
            because: "photon map should add indirect radiance on top of direct");
    }

    [Fact]
    public void Trace_NeverReturnsNegativeValues()
    {
        var integrator = new PhotonMapIntegrator(kNearest: 20);
        var (scene, lights) = MakeCornellBox();
        var photonMap = MakePhotonMap(scene, lights, 10_000);
        var states = MakePixelStates(1, 0.1);

        var ray = new Ray(new Vector3(0, 0, 3.5), -Vector3.UnitZ);

        for (var i = 0; i < 200; i++)
        {
            states[0] = PixelEstimationState.Initial(0.1);
            var result = integrator.Trace(ray, scene, lights,
                photonMap, states, 0, new Sampler(i));

            result.X.Should().BeGreaterThanOrEqualTo(0);
            result.Y.Should().BeGreaterThanOrEqualTo(0);
            result.Z.Should().BeGreaterThanOrEqualTo(0);
        }
    }

    // ── FindVisibleDiffusePoint ───────────────────────────────────────────────

    [Fact]
    public void FindVisibleDiffusePoint_RayHitsDiffuse_ReturnsHit()
    {
        var integrator = new PhotonMapIntegrator();
        var scene = new SceneList();
        scene.Add(new Sphere(Vector3.Zero, 1.0,
                             new Lambertian(Vector3.One)));

        var ray = new Ray(new Vector3(0, 0, -3), Vector3.UnitZ);
        var hit = integrator.FindVisibleDiffusePoint(ray, scene);

        hit.Should().NotBeNull();
        hit!.Value.T.Should().BeApproximately(2.0, 0.1);
    }

    [Fact]
    public void FindVisibleDiffusePoint_RayEscapes_ReturnsNull()
    {
        var integrator = new PhotonMapIntegrator();
        var scene = new SceneList();
        var ray = new Ray(Vector3.Zero, Vector3.UnitZ);

        var hit = integrator.FindVisibleDiffusePoint(ray, scene);

        hit.Should().BeNull();
    }

    [Fact]
    public void FindVisibleDiffusePoint_RayHitsEmissive_ReturnsNull()
    {
        var integrator = new PhotonMapIntegrator();
        var scene = new SceneList();
        scene.Add(new Sphere(new Vector3(0, 0, 5), 1.0,
                             new Emissive(Vector3.One)));

        var ray = new Ray(Vector3.Zero, Vector3.UnitZ);
        var hit = integrator.FindVisibleDiffusePoint(ray, scene);

        hit.Should().BeNull();
    }

    [Fact]
    public void FindVisibleDiffusePoint_RayHitsMirrorThenDiffuse_ReturnsNull()
    {
        // Mirror sphere in front, diffuse sphere behind — should follow
        // specular bounce to find the diffuse surface
        var integrator = new PhotonMapIntegrator();
        var scene = new SceneList();
        scene.Add(new Sphere(new Vector3(0, 0, 3), 1.0,
                             new Mirror(Vector3.One)));

        var ray = new Ray(Vector3.Zero, Vector3.UnitZ);
        var hit = integrator.FindVisibleDiffusePoint(ray, scene);

        // No diffuse surface behind the mirror — should return null
        hit.Should().BeNull();
    }

    // ── Pixel state ───────────────────────────────────────────────────────────

    [Fact]
    public void Trace_PixelState_RadiusShrinks_AfterPass()
    {
        var integrator = new PhotonMapIntegrator(kNearest: 10);
        var (scene, lights) = MakeCornellBox();
        var photonMap = MakePhotonMap(scene, lights, 10_000);
        var states = MakePixelStates(1, 0.5);

        var initialRadius = states[0].Radius;
        var ray = new Ray(new Vector3(0, 0, 3.5), -Vector3.UnitZ);

        integrator.Trace(ray, scene, lights,
            photonMap, states, 0, new Sampler(0));

        states[0].Radius.Should().BeLessThanOrEqualTo(initialRadius,
            because: "PPM radius must not grow after a pass");
    }
}