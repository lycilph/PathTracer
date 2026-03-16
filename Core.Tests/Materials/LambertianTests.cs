using Core.Materials;
using FluentAssertions;

namespace Core.Tests.Materials;

public class LambertianTests
{
    [Fact]
    public void Scatter_AlwaysScatters()
    {
        var mat = new Lambertian(new Vector3(0.8, 0.5, 0.3));
        var sampler = new Sampler(seed: 42);
        var ray = new Ray(new Vector3(0, 0, -1), Vector3.UnitZ);
        var hit = HitRecord.Create(1.0, Vector3.Zero, ray, -Vector3.UnitZ, mat);

        mat.Scatter(ray, hit, sampler, out _, out _).Should().BeTrue();
    }

    [Fact]
    public void Scatter_AttenuationEqualsAlbedo()
    {
        var albedo = new Vector3(0.8, 0.5, 0.3);
        var mat = new Lambertian(albedo);
        var sampler = new Sampler(seed: 42);
        var ray = new Ray(new Vector3(0, 0, -1), Vector3.UnitZ);
        var hit = HitRecord.Create(1.0, Vector3.Zero, ray, -Vector3.UnitZ, mat);

        mat.Scatter(ray, hit, sampler, out var attenuation, out _);

        attenuation.Should().Be(albedo);
    }

    [Fact]
    public void Scatter_ScatteredRayOriginIsHitPoint()
    {
        var mat = new Lambertian(Vector3.One);
        var sampler = new Sampler(seed: 42);
        var ray = new Ray(new Vector3(0, 0, -1), Vector3.UnitZ);
        var hit = HitRecord.Create(1.0, Vector3.Zero, ray, -Vector3.UnitZ, mat);

        mat.Scatter(ray, hit, sampler, out _, out var scattered);

        scattered.Origin.Should().Be(Vector3.Zero);
    }

    [Fact]
    public void Scatter_ScatteredDirectionInCorrectHemisphere()
    {
        var mat = new Lambertian(Vector3.One);
        var sampler = new Sampler(seed: 42);
        var ray = new Ray(new Vector3(0, 0, -1), Vector3.UnitZ);
        var hit = HitRecord.Create(1.0, Vector3.Zero, ray, -Vector3.UnitZ, mat);

        for (var i = 0; i < 100; i++)
        {
            mat.Scatter(ray, hit, sampler, out _, out var scattered);
            Vector3.Dot(scattered.Direction, hit.Normal)
                .Should().BeGreaterThanOrEqualTo(0.0,
                    because: "scattered ray must stay in the upper hemisphere");
        }
    }

    [Fact]
    public void EnergyConservation_IntegralOfBrdfCosTheta_LessThanOne()
    {
        // Monte Carlo estimate of ∫ f_r · cosθ dω = albedo (must be ≤ 1)
        // We verify by checking the average attenuation across many scatters
        var albedo = new Vector3(0.9, 0.7, 0.5);
        var mat = new Lambertian(albedo);
        var sampler = new Sampler(seed: 99);
        var ray = new Ray(new Vector3(0, 0, -1), Vector3.UnitZ);
        var hit = HitRecord.Create(1.0, Vector3.Zero, ray, -Vector3.UnitZ, mat);

        var total = Vector3.Zero;
        const int n = 10_000;

        for (var i = 0; i < n; i++)
        {
            mat.Scatter(ray, hit, sampler, out var atten, out _);
            total = total + atten;
        }

        var mean = total / n;

        // Each channel of the mean attenuation should equal albedo and be ≤ 1
        mean.X.Should().BeApproximately(albedo.X, 0.01);
        mean.Y.Should().BeApproximately(albedo.Y, 0.01);
        mean.Z.Should().BeApproximately(albedo.Z, 0.01);

        mean.X.Should().BeLessThanOrEqualTo(1.0);
        mean.Y.Should().BeLessThanOrEqualTo(1.0);
        mean.Z.Should().BeLessThanOrEqualTo(1.0);
    }
}