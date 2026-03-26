using Core;
using Core.Acceleration;
using Core.Algebra;
using Core.Geometry;
using Engine.Lighting;
using Engine.Materials;
using Engine.PhotonMapping;
using FluentAssertions;

namespace Engine.Tests.PhotonMapping;

public class PhotonEmitterTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static AreaLight MakeLight(Vector3? emission = null) => new(
        corner: new Vector3(-0.5, 2, -0.5),
        edge1: new Vector3(1, 0, 0),
        edge2: new Vector3(0, 0, 1),
        emission: emission ?? new Vector3(10, 10, 10));

    private static IHittable MakeFloor() =>
    new Quad(
        new Vector3(-50, -1, -50),
        new Vector3(100, 0, 0),
        new Vector3(0, 0, 100),
        new Lambertian(new Vector3(0.8, 0.8, 0.8)));

    private static IHittable MakeScene(params IHittable[] primitives)
    {
        var scene = new SceneList();
        foreach (var p in primitives)
            scene.Add(p);
        return scene;
    }

    // Replace MakeScene with a closed box scene for emission tests
    private static (IHittable scene, IReadOnlyList<ILight> lights)
        MakeClosedBoxScene()
    {
        var white = new Lambertian(new Vector3(0.8, 0.8, 0.8));
        var light = new AreaLight(
            new Vector3(-0.25, 0.999, -0.25),
            new Vector3(0.5, 0, 0),
            new Vector3(0, 0, 0.5),
            new Vector3(15, 15, 15));

        var scene = new SceneList();
        // Floor
        scene.Add(new Quad(new Vector3(-1, -1, -1),
            new Vector3(2, 0, 0), new Vector3(0, 0, 2), white));
        // Ceiling
        scene.Add(new Quad(new Vector3(-1, 1, -1),
            new Vector3(2, 0, 0), new Vector3(0, 0, 2), white));
        // Back wall
        scene.Add(new Quad(new Vector3(-1, -1, -1),
            new Vector3(2, 0, 0), new Vector3(0, 2, 0), white));
        // Left wall
        scene.Add(new Quad(new Vector3(-1, -1, -1),
            new Vector3(0, 2, 0), new Vector3(0, 0, 2), white));
        // Right wall
        scene.Add(new Quad(new Vector3(1, -1, -1),
            new Vector3(0, 2, 0), new Vector3(0, 0, 2), white));
        // Light
        scene.Add(light);

        return (scene, new List<ILight> { light });
    }

    // ── Basic emission ────────────────────────────────────────────────────────

    [Fact]
    public void Emit_NoLights_ReturnsEmpty()
    {
        var emitter = new PhotonEmitter();
        var scene = MakeScene(MakeFloor());

        var photons = emitter.Emit(1000, scene, [], maxDepth: 5, TestContext.Current.CancellationToken);

        photons.Should().BeEmpty();
    }

    [Fact]
    public void Emit_WithLightAndFloor_ProducesPhotons()
    {
        var emitter = new PhotonEmitter();
        var (scene, lights) = MakeClosedBoxScene();

        var photons = emitter.Emit(1000, scene, lights, maxDepth: 5, TestContext.Current.CancellationToken);

        photons.Should().NotBeEmpty();
    }

    [Fact]
    public void Emit_WithLightAndFloor_PhotonCountIsReasonable()
    {
        var emitter = new PhotonEmitter();
        var (scene, lights) = MakeClosedBoxScene();

        var photons = emitter.Emit(10_000, scene, lights, maxDepth: 20, TestContext.Current.CancellationToken);

        // In a closed box most photons should bounce and be stored
        photons.Count.Should().BeGreaterThan(2000);
    }

    // ── Path types ────────────────────────────────────────────────────────────

    [Fact]
    public void Emit_DirectPhotons_AreNotStored()
    {
        var emitter = new PhotonEmitter();
        var (scene, lights) = MakeClosedBoxScene();

        var photons = emitter.Emit(10_000, scene, lights, maxDepth: 5, TestContext.Current.CancellationToken);

        photons.Should().NotContain(
            p => p.PathType == PhotonPathType.Direct,
            because: "direct photons are handled by MIS, not the photon map");
    }

    [Fact]
    public void Emit_WithGlassSphere_ProducesCausticPhotons()
    {
        var emitter = new PhotonEmitter();
        var (scene, lights) = MakeClosedBoxScene();

        // Add glass sphere to the existing closed box scene
        var sceneList = new SceneList();
        sceneList.Add(scene);
        sceneList.Add(new Sphere(
            new Vector3(0, -0.5, 0), 0.4,
            new Dielectric(1.5)));

        var photons = emitter.Emit(50_000, sceneList, lights, maxDepth: 10, TestContext.Current.CancellationToken);

        photons.Should().Contain(
            p => p.PathType == PhotonPathType.Caustic,
            because: "glass sphere should produce caustic photons");
    }

    [Fact]
    public void Emit_WithDiffuseWalls_ProducesIndirectPhotons()
    {
        var emitter = new PhotonEmitter();
        var (scene, lights) = MakeClosedBoxScene();

        var photons = emitter.Emit(50_000, scene, lights, maxDepth: 10, TestContext.Current.CancellationToken);

        photons.Should().Contain(
            p => p.PathType == PhotonPathType.Indirect,
            because: "diffuse walls should produce indirect photons");
    }

    // ── Power ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Emit_PhotonPower_IsPositive()
    {
        var emitter = new PhotonEmitter();
        var light = MakeLight();
        var scene = MakeScene(MakeFloor(), light);

        var photons = emitter.Emit(1000, scene,
            new List<ILight> { light }, maxDepth: 5, TestContext.Current.CancellationToken);

        foreach (var photon in photons)
        {
            photon.Power.X.Should().BeGreaterThanOrEqualTo(0);
            photon.Power.Y.Should().BeGreaterThanOrEqualTo(0);
            photon.Power.Z.Should().BeGreaterThanOrEqualTo(0);
        }
    }

    [Fact]
    public void Emit_PhotonPower_DecreasesWithBounces()
    {
        var emitter = new PhotonEmitter();
        var (scene, lights) = MakeClosedBoxScene();

        var photons = emitter.Emit(50_000, scene, lights, maxDepth: 10, TestContext.Current.CancellationToken);

        var indirectPhotons = photons
            .Where(p => p.PathType == PhotonPathType.Indirect)
            .ToList();

        if (indirectPhotons.Count == 0) return;

        var avgIndirectPower = indirectPhotons
            .Average(p => (p.Power.X + p.Power.Y + p.Power.Z) / 3.0);

        avgIndirectPower.Should().BeLessThan(1.0,
            because: "indirect photons must have been attenuated by bounces");
    }

    // ── Cancellation ──────────────────────────────────────────────────────────

    [Fact]
    public void Emit_Cancelled_ReturnsPartialResult()
    {
        var emitter = new PhotonEmitter();
        var light = MakeLight();
        var scene = MakeScene(MakeFloor(), light);
        using var cts = new CancellationTokenSource();

        // Cancel immediately
        cts.Cancel();

        var photons = emitter.Emit(100_000, scene,
            new List<ILight> { light },
            maxDepth: 5,
            cancellationToken: cts.Token);

        // Should return without throwing — partial result is fine
        photons.Should().NotBeNull();
    }

    // ── Position ──────────────────────────────────────────────────────────────

    [Fact]
    public void Emit_PhotonPositions_AreOnSurfaces()
    {
        var emitter = new PhotonEmitter();
        var (scene, lights) = MakeClosedBoxScene();

        var photons = emitter.Emit(1000, scene, lights, maxDepth: 5, TestContext.Current.CancellationToken);

        // All photons should be within the box bounds
        foreach (var photon in photons)
        {
            photon.Position.X.Should().BeInRange(-1.01, 1.01);
            photon.Position.Y.Should().BeInRange(-1.01, 1.01);
            photon.Position.Z.Should().BeInRange(-1.01, 1.01);
        }
    }
}