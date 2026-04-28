using Core.Materials;
using Core.Math;
using Core.Random;
using Core.Sampling;
using Core.Scene;

namespace Tests.Materials;

public class DeltaMaterialsTests
{
    [Fact]
    public void Mirror_Sample_ReflectsCorrectly()
    {
        var mirror = new Mirror(new Vec3(1, 1, 1));
        var sampler = new Sampler(new Pcg32(1));

        var rayIn = new Ray(Vec3.Zero, -Vec3.UnitY);
        var hit = new HitRecord(Vec3.Zero, Vec3.UnitY, 1f, rayIn, mirror);
        var wo = (-rayIn.Direction).Normalized();

        Assert.True(mirror.Sample(wo, hit, sampler, out var wi, out var pdf, out var f));
        Assert.Equal(1f, pdf);
        Assert.True(wi.Y > 0.999f);
        Assert.False(f.NearZero());
    }

    [Fact]
    public void Dielectric_Sample_ReturnsValidDirection()
    {
        var glass = new Dielectric(1.5f);
        var sampler = new Sampler(new Pcg32(2));

        var rayIn = new Ray(Vec3.Zero, -Vec3.UnitY);
        var hit = new HitRecord(Vec3.Zero, Vec3.UnitY, 1f, rayIn, glass);
        var wo = (-rayIn.Direction).Normalized();

        Assert.True(glass.Sample(wo, hit, sampler, out var wi, out var pdf, out var f));
        Assert.Equal(1f, pdf);
        Assert.InRange(wi.Length(), 0.999f, 1.001f);
        Assert.False(f.NearZero());
    }
}