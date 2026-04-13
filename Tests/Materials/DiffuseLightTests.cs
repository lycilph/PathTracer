using Core.Materials;
using Core.Math;
using Core.Scene;

namespace Tests.Materials;

public class DiffuseLightTests
{
    [Fact]
    public void Emitted_IsZeroOnBackFace()
    {
        var light = new DiffuseLight(new Vec3(5, 6, 7));
        var outward = Vec3.UnitZ;
        var ray = new Ray(Vec3.Zero, Vec3.UnitZ);
        var hit = new HitRecord(new Vec3(0, 0, 1), outward, 1f, ray, light);

        Assert.False(hit.FrontFace);
        Assert.Equal(Vec3.Zero, light.Emitted(ray, hit));
    }

    [Fact]
    public void Emitted_IsRadianceOnFrontFace()
    {
        var light = new DiffuseLight(new Vec3(5, 6, 7));
        var outward = Vec3.UnitZ;
        var ray = new Ray(Vec3.Zero, -Vec3.UnitZ);
        var hit = new HitRecord(new Vec3(0, 0, 1), outward, 1f, ray, light);

        Assert.True(hit.FrontFace);
        Assert.Equal(new Vec3(5, 6, 7), light.Emitted(ray, hit));
    }
}