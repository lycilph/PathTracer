using Core.Materials;
using Core.Math;
using Core.Random;
using Core.Sampling;
using Core.Scene;

namespace Tests.Materials;

public class LambertianSamplingTests
{
    [Fact]
    public void Scatter_AlwaysReturnsTrue()
    {
        var lambert = new Lambertian(new Vec3(0.8f, 0.7f, 0.6f));
        var sampler = new Sampler(new Pcg32(1));

        var rayIn = new Ray(Vec3.Zero, Vec3.UnitZ);
        var hit = new HitRecord(
            point: Vec3.Zero,
            outwardNormal: Vec3.UnitY,
            t: 1f,
            ray: rayIn,
            material: lambert);

        bool scattered = lambert.Scatter(
            rayIn,
            hit,
            sampler,
            out _,
            out _);

        Assert.True(scattered);
    }

    [Fact]
    public void Scatter_ProducesDirectionInUpperHemisphere()
    {
        var lambert = new Lambertian(new Vec3(1f, 1f, 1f));
        var sampler = new Sampler(new Pcg32(42));

        var rayIn = new Ray(Vec3.Zero, Vec3.UnitZ);
        var hit = new HitRecord(
            point: Vec3.Zero,
            outwardNormal: Vec3.UnitY,
            t: 1f,
            ray: rayIn,
            material: lambert);

        for (int i = 0; i < 10_000; i++)
        {
            lambert.Scatter(rayIn, hit, sampler, out var scattered, out _);

            float cos = Vec3.Dot(scattered.Direction.Normalized(), hit.Normal);
            Assert.True(cos >= 0f);
        }
    }

    [Fact]
    public void Scatter_AttenuationEqualsAlbedo()
    {
        var albedo = new Vec3(0.2f, 0.4f, 0.6f);
        var lambert = new Lambertian(albedo);
        var sampler = new Sampler(new Pcg32(7));

        var rayIn = new Ray(Vec3.Zero, Vec3.UnitZ);
        var hit = new HitRecord(
            point: Vec3.Zero,
            outwardNormal: Vec3.UnitY,
            t: 1f,
            ray: rayIn,
            material: lambert);

        lambert.Scatter(rayIn, hit, sampler, out _, out var attenuation);

        Assert.Equal(albedo, attenuation);
    }

    [Fact]
    public void Scatter_ProducesNormalizedDirection()
    {
        var lambert = new Lambertian(new Vec3(1f));
        var sampler = new Sampler(new Pcg32(123));

        var rayIn = new Ray(Vec3.Zero, Vec3.UnitZ);
        var hit = new HitRecord(
            point: Vec3.Zero,
            outwardNormal: Vec3.UnitY,
            t: 1f,
            ray: rayIn,
            material: lambert);

        lambert.Scatter(rayIn, hit, sampler, out var scattered, out _);

        float len = scattered.Direction.Length();
        Assert.InRange(len, 0.999f, 1.001f);
    }
}
