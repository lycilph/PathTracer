using Core.Materials;
using Core.Math;
using Core.Scene;

namespace Tests.Scene;

public class SphereTests
{
    [Fact]
    public void SphereHit_CenterRay_HitsAtExpectedT()
    {
        var mat = new Lambertian(new Vec3(1, 1, 1));
        var s = new Sphere(new Vec3(0, 0, -1), 0.5f, mat);
        var r = new Ray(new Vec3(0, 0, 0), new Vec3(0, 0, -1));

        bool hit = s.Hit(r, 0.001f, 1000f, out var rec);
        Assert.True(hit);

        Assert.InRange(rec.T, 0.499f, 0.501f);
        Assert.True(rec.FrontFace);
        Assert.InRange(rec.Normal.Z, 0.999f, 1.001f);
        Assert.Same(mat, rec.Material);
    }

    [Fact]
    public void SphereMiss_OffsetRay_DoesNotHit()
    {
        var mat = new Lambertian(new Vec3(1, 1, 1));
        var s = new Sphere(new Vec3(0, 0, -1), 0.5f, mat);
        var r = new Ray(new Vec3(0, 2, 0), new Vec3(0, 0, -1));

        Assert.False(s.Hit(r, 0.001f, 1000f, out _));
    }
}