using Core.Math;
using Core.Materials;
using Core.Scene;
using Xunit;

namespace Tests.Scene;

public class TranslateScaleTests
{
    [Fact]
    public void Translate_ShiftsHitPoint()
    {
        var mat = new Lambertian(new Vec3(1,1,1));
        IHittable sphere = new Sphere(new Vec3(0,0,-5), 1f, mat);
        IHittable moved = new Translate(sphere, new Vec3(10, 0, 0));

        var ray = new Ray(new Vec3(10,0,0), new Vec3(0,0,-1));
        Assert.True(moved.Hit(ray, 0.001f, 1000f, out var hit));

        // Should hit around z=-4 (center -5, radius 1)
        Assert.InRange(hit.Point.X, 9.999f, 10.001f);
        Assert.InRange(hit.Point.Z, -4.001f, -3.999f);
    }

    [Fact]
    public void Scale_ChangesBoundingBoxSize()
    {
        var mat = new Lambertian(new Vec3(1,1,1));
        IHittable sphere = new Sphere(new Vec3(0,0,-5), 1f, mat);
        IHittable scaled = new Scale(sphere, 2f);

        Assert.True(sphere.BoundingBox(out var b0));
        Assert.True(scaled.BoundingBox(out var b1));

        // extents doubled
        var ext0 = b0.Max - b0.Min;
        var ext1 = b1.Max - b1.Min;
        Assert.InRange(ext1.X / ext0.X, 1.999f, 2.001f);
        Assert.InRange(ext1.Y / ext0.Y, 1.999f, 2.001f);
        Assert.InRange(ext1.Z / ext0.Z, 1.999f, 2.001f);
    }
}
