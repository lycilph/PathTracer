using Core.Math;
using Core.Scene;

namespace Tracer.Tests.Scene;

public class AabbTests
{
    [Fact]
    public void Aabb_Hit_Works()
    {
        var box = new Aabb(new Vec3(-1,-1,-1), new Vec3(1,1,1));
        var ray = new Ray(new Vec3(0,0,-5), new Vec3(0,0,1));
        Assert.True(box.Hit(ray, 0.001f, 1000f));
    }

    [Fact]
    public void Aabb_Miss_Works()
    {
        var box = new Aabb(new Vec3(-1,-1,-1), new Vec3(1,1,1));
        var ray = new Ray(new Vec3(5,5,-5), new Vec3(0,0,1));
        Assert.False(box.Hit(ray, 0.001f, 1000f));
    }
}
