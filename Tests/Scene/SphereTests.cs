using Core.Math;
using Core.Scene;

namespace Tests.Scene;

public class SphereTests
{
    [Fact]
    public void SphereHit_CenterRay_HitsAtExpectedT()
    {
        // Sphere centered at (0,0,-1) radius 0.5
        var s = new Sphere(new Vec3(0, 0, -1), 0.5f);
        var r = new Ray(new Vec3(0, 0, 0), new Vec3(0, 0, -1));

        bool hit = s.Hit(r, 0.001f, 1000f, out var rec);
        Assert.True(hit);

        // Expected near intersection at z=-0.5 => t=0.5
        Assert.InRange(rec.T, 0.499f, 0.501f);
        Assert.True(rec.FrontFace);

        // Normal at hit point should point toward +Z
        Assert.InRange(rec.Normal.Z, 0.999f, 1.001f);
    }

    [Fact]
    public void SphereMiss_OffsetRay_DoesNotHit()
    {
        var s = new Sphere(new Vec3(0, 0, -1), 0.5f);
        var r = new Ray(new Vec3(0, 2, 0), new Vec3(0, 0, -1));

        bool hit = s.Hit(r, 0.001f, 1000f, out _);
        Assert.False(hit);
    }
}