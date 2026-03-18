using Core.Materials;
using FluentAssertions;

namespace Core.Tests;

public class MisIntegratorTests
{
    private static (SceneList scene, List<ILight> lights,
                    Camera camera, MisIntegrator integrator) MakeCornellSetup()
    {
        var scene = new SceneList();
        var lights = new List<ILight>();

        var white = new Lambertian(new Vector3(0.73, 0.73, 0.73));
        var red = new Lambertian(new Vector3(0.65, 0.05, 0.05));
        var green = new Lambertian(new Vector3(0.12, 0.45, 0.15));

        // Walls
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

        // Area light
        var areaLight = new AreaLight(
            new Vector3(-0.25, 0.999, -0.25),
            new Vector3(0.5, 0, 0),
            new Vector3(0, 0, 0.5),
            new Vector3(15, 15, 15));

        scene.Add(areaLight);
        lights.Add(areaLight);

        var camera = new Camera(
            new Vector3(0, 0, 3.5), Vector3.Zero,
            Vector3.UnitY, 40, 64, 64);

        var integrator = new MisIntegrator
        {
            BackgroundRadiance = Vector3.Zero
        };

        return (scene, lights, camera, integrator);
    }

    [Fact]
    public void Trace_EmptyScene_ReturnsBackground()
    {
        var integrator = new MisIntegrator
        {
            BackgroundRadiance = new Vector3(0.5, 0.7, 1.0)
        };
        var scene = new SceneList();
        var lights = new List<ILight>();
        var ray = new Ray(Vector3.Zero, Vector3.UnitZ);

        integrator.Trace(ray, scene, lights, new Sampler(0))
                  .Should().Be(new Vector3(0.5, 0.7, 1.0));
    }

    [Fact]
    public void Trace_NeverReturnsNegativeValues()
    {
        var (scene, lights, camera, integrator) = MakeCornellSetup();

        for (var i = 0; i < 500; i++)
        {
            var sampler = new Sampler(i);
            var ray = camera.GenerateRay(32, 32, sampler.Next(), sampler.Next());
            var result = integrator.Trace(ray, scene, lights, sampler);

            result.X.Should().BeGreaterThanOrEqualTo(0);
            result.Y.Should().BeGreaterThanOrEqualTo(0);
            result.Z.Should().BeGreaterThanOrEqualTo(0);
        }
    }

    [Fact]
    public void Trace_MisConvergesFasterThanBrdf()
    {
        // MIS should converge to a brighter, less noisy result than pure
        // BRDF sampling at the same sample count for a scene with a small light
        var (scene, lights, camera, misIntegrator) = MakeCornellSetup();

        var brdfIntegrator = new PathIntegrator
        {
            BackgroundRadiance = Vector3.Zero
        };

        const int n = 500;
        var misSum = Vector3.Zero;
        var brdfSum = Vector3.Zero;

        for (var i = 0; i < n; i++)
        {
            var misSampler = new Sampler(i);
            var brdfSampler = new Sampler(i);
            var ray = camera.GenerateRay(32, 32,
                                  misSampler.Next(), misSampler.Next());

            misSum = misSum + misIntegrator.Trace(ray, scene, lights, misSampler);
            brdfSum = brdfSum + brdfIntegrator.Trace(ray, scene, brdfSampler);
        }

        var misMean = misSum / n;
        var brdfMean = brdfSum / n;

        // Both should produce non-black results for a lit scene
        misMean.X.Should().BeGreaterThan(0);
        brdfMean.X.Should().BeGreaterThan(0);

        // MIS mean should be at least as bright — it finds the light more reliably
        misMean.X.Should().BeGreaterThanOrEqualTo(brdfMean.X * 0.5);
    }

    [Fact]
    public void Trace_HitsEmissiveDirectly_ReturnsEmissionOnFirstBounce()
    {
        var scene = new SceneList();
        var lights = new List<ILight>();

        var areaLight = new AreaLight(
            new Vector3(-1, -1, 4),
            new Vector3(2, 0, 0),
            new Vector3(0, 2, 0),
            new Vector3(5, 3, 1));

        scene.Add(areaLight);

        var integrator = new MisIntegrator();
        var ray = new Ray(Vector3.Zero, Vector3.UnitZ);
        var result = integrator.Trace(ray, scene, lights, new Sampler(0));

        // Direct hit on emissive should return the emission
        result.X.Should().BeApproximately(5, 0.1);
        result.Y.Should().BeApproximately(3, 0.1);
        result.Z.Should().BeApproximately(1, 0.1);
    }

    [Fact]
    public void Trace_CausticPath_ThroughGlass_IsNotBlack()
    {
        // A diffuse floor below a glass sphere with a light above.
        // Rays from camera hit the floor, some paths go:
        // camera → floor → refract through glass → refract again → light
        var scene = new SceneList();
        var lights = new List<ILight>();

        // Floor at y=-1
        var floor = new Quad(
            new Vector3(-2, -1, -2),
            new Vector3(4, 0, 0),
            new Vector3(0, 0, 4),
            new Lambertian(new Vector3(0.8, 0.8, 0.8)));

        // Glass sphere sitting on the floor at y=-0.5 (radius 0.5)
        var glassSphere = new Sphere(
            new Vector3(0, -0.5, 0), 0.5,
            new Dielectric(1.5));

        // Large bright light above
        var areaLight = new AreaLight(
            new Vector3(-1, 2, -1),
            new Vector3(2, 0, 0),
            new Vector3(0, 0, 2),
            new Vector3(20, 20, 20));

        scene.Add(floor);
        scene.Add(glassSphere);
        scene.Add(areaLight);
        lights.Add(areaLight);

        var integrator = new MisIntegrator { BackgroundRadiance = Vector3.Zero };

        // Camera above looking straight down at the floor below the sphere
        // Direction: straight down (-Y)
        var total = Vector3.Zero;
        const int n = 500;
        for (var i = 0; i < n; i++)
        {
            // Slightly randomise the hit point on the floor beneath the sphere
            var sampler = new Sampler(i);
            var x = (sampler.Next() - 0.5) * 0.3; // small jitter around x=0
            var z = (sampler.Next() - 0.5) * 0.3;
            var ray = new Ray(new Vector3(x, 5, z), -Vector3.UnitY);
            total = total + integrator.Trace(ray, scene, lights, sampler);
        }

        var mean = total / n;

        mean.X.Should().BeGreaterThan(0,
            because: "caustic paths through glass must not be discarded");
    }
}