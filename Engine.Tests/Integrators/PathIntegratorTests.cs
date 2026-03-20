using Core.Acceleration;
using Core.Algebra;
using Core.Geometry;
using Core.Sampling;
using Engine.Materials;
using Engine.Rendering;
using FluentAssertions;

namespace Engine.Tests.Integrators;

public class PathIntegratorTests
{
    //private static readonly Sampler Sampler = new(seed: 42);

    // ── Background ────────────────────────────────────────────────────────────

    [Fact]
    public void Trace_EmptyScene_ReturnsBackground()
    {
        var integrator = new PathIntegrator
        {
            BackgroundRadiance = new Vector3(0.5, 0.7, 1.0)
        };
        var scene = new SceneList();
        var ray = new Ray(Vector3.Zero, Vector3.UnitZ);

        var result = integrator.Trace(ray, scene, new Sampler(0));

        result.Should().Be(new Vector3(0.5, 0.7, 1.0));
    }

    [Fact]
    public void Trace_EmptySceneBlackBackground_ReturnsZero()
    {
        var integrator = new PathIntegrator { BackgroundRadiance = Vector3.Zero };
        var scene = new SceneList();
        var ray = new Ray(Vector3.Zero, Vector3.UnitZ);

        var result = integrator.Trace(ray, scene, new Sampler(0));

        result.Should().Be(Vector3.Zero);
    }

    // ── Emissive surfaces ─────────────────────────────────────────────────────

    [Fact]
    public void Trace_RayHitsEmissiveSphere_ReturnsEmission()
    {
        var emission = new Vector3(5, 3, 1);
        var scene = new SceneList();
        scene.Add(new Sphere(new Vector3(0, 0, 5), 1.0, new Emissive(emission)));

        var integrator = new PathIntegrator();
        var ray = new Ray(Vector3.Zero, Vector3.UnitZ);

        var result = integrator.Trace(ray, scene, new Sampler(0));

        result.Should().Be(emission);
    }

    // ── Diffuse surfaces ──────────────────────────────────────────────────────

    [Fact]
    public void Trace_BlackBackground_DiffuseSphere_ReturnsZero()
    {
        // A diffuse sphere with no light source — all paths escape to black
        var scene = new SceneList();
        scene.Add(new Sphere(Vector3.Zero, 0.5, new Lambertian(new Vector3(0.8, 0.5, 0.3))));

        var integrator = new PathIntegrator { BackgroundRadiance = Vector3.Zero };
        var ray = new Ray(new Vector3(0, 0, -2), Vector3.UnitZ);

        // Average over many samples — should converge to black
        var total = Vector3.Zero;
        const int n = 1000;
        for (var i = 0; i < n; i++)
            total = total + integrator.Trace(ray, scene, new Sampler(i));

        var mean = total / n;
        mean.X.Should().BeApproximately(0.0, 0.01);
        mean.Y.Should().BeApproximately(0.0, 0.01);
        mean.Z.Should().BeApproximately(0.0, 0.01);
    }

    // ── Furnace test ──────────────────────────────────────────────────────────

    [Fact]
    public void FurnaceTest_Lambertian_EnergyConserved()
    {
        // §7.2 Furnace test: a Lambertian sphere with albedo=1 inside a
        // uniformly emitting environment (background = 1) must return
        // exactly 1 — no energy created or destroyed.
        var scene = new SceneList();
        scene.Add(new Sphere(Vector3.Zero, 1.0, new Lambertian(Vector3.One)));

        var integrator = new PathIntegrator
        {
            BackgroundRadiance = Vector3.One,
            MinDepth = 3,
            MaxDepth = 50
        };

        var total = Vector3.Zero;
        const int n = 5000;

        for (var i = 0; i < n; i++)
        {
            // Rays from outside aimed at the sphere
            var ray = new Ray(new Vector3(0, 0, -3), Vector3.UnitZ);
            total = total + integrator.Trace(ray, scene, new Sampler(i));
        }

        var mean = total / n;

        // Must be within a few percent of 1.0 on each channel
        mean.X.Should().BeApproximately(1.0, 0.05);
        mean.Y.Should().BeApproximately(1.0, 0.05);
        mean.Z.Should().BeApproximately(1.0, 0.05);
    }

    // ── Mirror ────────────────────────────────────────────────────────────────

    [Fact]
    public void Trace_MirrorSphere_ReflectsBackgroundColour()
    {
        // A perfect mirror sphere should reflect the background
        var background = new Vector3(0.2, 0.5, 0.9);
        var scene = new SceneList();
        scene.Add(new Sphere(Vector3.Zero, 1.0, new Mirror(Vector3.One)));

        var integrator = new PathIntegrator { BackgroundRadiance = background };
        var ray = new Ray(new Vector3(0, 0, -3), Vector3.UnitZ);

        var total = Vector3.Zero;
        const int n = 200;
        for (var i = 0; i < n; i++)
            total = total + integrator.Trace(ray, scene, new Sampler(i));

        var mean = total / n;

        // Mirror with full reflectance against a uniform background → background colour
        mean.X.Should().BeApproximately(background.X, 0.05);
        mean.Y.Should().BeApproximately(background.Y, 0.05);
        mean.Z.Should().BeApproximately(background.Z, 0.05);
    }

    // ── Result validity ───────────────────────────────────────────────────────

    [Fact]
    public void Trace_NeverReturnsNegativeValues()
    {
        var scene = new SceneList();
        scene.Add(new Sphere(new Vector3(0, 0, 3), 1.0,
            new Lambertian(new Vector3(0.8, 0.5, 0.3))));
        scene.Add(new Sphere(new Vector3(0, 2, 3), 0.5,
            new Emissive(new Vector3(10, 10, 10))));

        var integrator = new PathIntegrator();
        var ray = new Ray(Vector3.Zero, Vector3.UnitZ);

        for (var i = 0; i < 500; i++)
        {
            var result = integrator.Trace(ray, scene, new Sampler(i));
            result.X.Should().BeGreaterThanOrEqualTo(0);
            result.Y.Should().BeGreaterThanOrEqualTo(0);
            result.Z.Should().BeGreaterThanOrEqualTo(0);
        }
    }
}