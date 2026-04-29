using Core.Materials;
using Core.Math;
using Core.Scene;

namespace Tests.Scene;

public class MovingSphereTests
{
    [Fact]
    public void Center_InterpolatesLinearly()
    {
        var mat = new Lambertian(new Vec3(1, 1, 1));
        var ms = new MovingSphere(
            center0: new Vec3(0, 0, 0),
            center1: new Vec3(10, 0, 0),
            time0: 0f,
            time1: 1f,
            radius: 1f,
            material: mat);

        Assert.Equal(new Vec3(0, 0, 0), ms.Center(0f));
        Assert.Equal(new Vec3(10, 0, 0), ms.Center(1f));

        var mid = ms.Center(0.5f);
        Assert.InRange(mid.X, 4.999f, 5.001f);
    }

    [Fact]
    public void BoundingBox_EnclosesEndpoints()
    {
        var mat = new Lambertian(new Vec3(1, 1, 1));
        var ms = new MovingSphere(
            center0: new Vec3(-2, 0, 0),
            center1: new Vec3(3, 0, 0),
            time0: 0f,
            time1: 1f,
            radius: 1f,
            material: mat);

        Assert.True(ms.BoundingBox(out var box));
        Assert.True(box.Min.X <= -3f);
        Assert.True(box.Max.X >= 4f);
    }

    [Fact]
    public void Hit_DependsOnRayTime()
    {
        var mat = new Lambertian(new Vec3(1, 1, 1));
        var ms = new MovingSphere(
            center0: new Vec3(-2, 0, -5),
            center1: new Vec3(2, 0, -5),
            time0: 0f,
            time1: 1f,
            radius: 1f,
            material: mat);

        // Ray at time 0 should hit near x=-2
        var r0 = new Ray(new Vec3(-2, 0, 0), new Vec3(0, 0, -1), time: 0f);
        Assert.True(ms.Hit(r0, 0.001f, 1000f, out var h0));
        Assert.InRange(h0.Point.X, -2.001f, -1.999f);

        // Ray at time 1 should hit near x=+2
        var r1 = new Ray(new Vec3(2, 0, 0), new Vec3(0, 0, -1), time: 1f);
        Assert.True(ms.Hit(r1, 0.001f, 1000f, out var h1));
        Assert.InRange(h1.Point.X, 1.999f, 2.001f);
    }
}