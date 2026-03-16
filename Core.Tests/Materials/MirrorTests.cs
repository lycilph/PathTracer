using Core.Materials;
using FluentAssertions;

namespace Core.Tests.Materials;

public class MirrorTests
{
    [Fact]
    public void Reflect_RayPerpendicularToSurface_ReflectsBack()
    {
        // Ray hitting a flat surface straight on should bounce straight back
        var reflected = Mirror.Reflect(-Vector3.UnitY, Vector3.UnitY);
        reflected.Should().Be(Vector3.UnitY);
    }

    [Fact]
    public void Reflect_FortyFiveDegrees_IsCorrect()
    {
        // Ray coming in at 45° in XY plane off a Y-normal surface
        // Incoming: (1, -1, 0) normalised. Outgoing should be (1, 1, 0) normalised
        var incoming = new Vector3(1, -1, 0).Normalize();
        var normal = Vector3.UnitY;
        var reflected = Mirror.Reflect(incoming, normal);
        var expected = new Vector3(1, 1, 0).Normalize();

        reflected.X.Should().BeApproximately(expected.X, 1e-10);
        reflected.Y.Should().BeApproximately(expected.Y, 1e-10);
        reflected.Z.Should().BeApproximately(expected.Z, 1e-10);
    }

    [Fact]
    public void Reflect_PreservesLength()
    {
        var incoming = new Vector3(1, -2, 0.5).Normalize();
        var reflected = Mirror.Reflect(incoming, Vector3.UnitY);
        reflected.Length.Should().BeApproximately(1.0, 1e-10);
    }

    [Fact]
    public void Scatter_AttenuationEqualsReflectance()
    {
        var reflectance = new Vector3(0.9, 0.9, 0.9);
        var mat = new Mirror(reflectance);
        var sampler = new Sampler(seed: 0);
        var ray = new Ray(new Vector3(0, 1, -1), new Vector3(0, -1, 1).Normalize());
        var hit = HitRecord.Create(1.0, Vector3.Zero, ray, Vector3.UnitY, mat);

        mat.Scatter(ray, hit, sampler, out var attenuation, out _);

        attenuation.Should().Be(reflectance);
    }

    [Fact]
    public void Scatter_ReflectedDirectionIsCorrect()
    {
        var mat = new Mirror(Vector3.One);
        var sampler = new Sampler(seed: 0);

        // Ray coming in at 45° to a Y-normal surface
        var direction = new Vector3(0, -1, 1).Normalize();
        var ray = new Ray(new Vector3(0, 1, -1), direction);
        var hit = HitRecord.Create(1.0, Vector3.Zero, ray, Vector3.UnitY, mat);

        mat.Scatter(ray, hit, sampler, out _, out var scattered);

        // Reflected direction should have same X/Z but flipped Y
        scattered.Direction.X.Should().BeApproximately(direction.X, 1e-10);
        scattered.Direction.Y.Should().BeApproximately(-direction.Y, 1e-10);
        scattered.Direction.Z.Should().BeApproximately(direction.Z, 1e-10);
    }

    [Fact]
    public void Scatter_ScatteredRayOriginIsHitPoint()
    {
        var mat = new Mirror(Vector3.One);
        var sampler = new Sampler(seed: 0);
        var ray = new Ray(new Vector3(0, 1, -1), new Vector3(0, -1, 1).Normalize());
        var hit = HitRecord.Create(1.0, Vector3.Zero, ray, Vector3.UnitY, mat);

        mat.Scatter(ray, hit, sampler, out _, out var scattered);

        scattered.Origin.Should().Be(Vector3.Zero);
    }

    [Fact]
    public void Scatter_IsDeterministic()
    {
        // Mirror has no randomness — calling twice must give identical results
        var mat = new Mirror(Vector3.One);
        var ray = new Ray(new Vector3(0, 1, -1), new Vector3(0, -1, 1).Normalize());
        var hit = HitRecord.Create(1.0, Vector3.Zero, ray, Vector3.UnitY, mat);

        mat.Scatter(ray, hit, new Sampler(seed: 1), out _, out var scattered1);
        mat.Scatter(ray, hit, new Sampler(seed: 2), out _, out var scattered2);

        scattered1.Direction.Should().Be(scattered2.Direction);
    }
}