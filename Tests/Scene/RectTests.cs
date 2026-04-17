using Core.Materials;
using Core.Math;
using Core.Scene;

namespace Tests.Scene;

public class RectTests
{
    [Fact]
    public void XYRect_Hit_Works()
    {
        var mat = new Lambertian(new Vec3(1, 1, 1));
        var rect = new XYRect(0, 1, 0, 1, k: 2, mat);

        var ray = new Ray(new Vec3(0.5f, 0.5f, 0), new Vec3(0, 0, 1));
        Assert.True(rect.Hit(ray, 0.001f, 1000f, out var rec));
        Assert.InRange(rec.T, 1.999f, 2.001f);

        Assert.True(rect.BoundingBox(out var box));
        Assert.True(box.Min.Z < 2f && box.Max.Z > 2f);
    }

    [Fact]
    public void XYRect_Miss_Works()
    {
        var mat = new Lambertian(new Vec3(1, 1, 1));
        var rect = new XYRect(0, 1, 0, 1, k: 2, mat);

        var ray = new Ray(new Vec3(2f, 2f, 0), new Vec3(0, 0, 1));
        Assert.False(rect.Hit(ray, 0.001f, 1000f, out _));
    }
}