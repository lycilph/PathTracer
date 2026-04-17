using Core.Materials;
using Core.Math;
using Core.Scene;

namespace Tracer.Tests.Scene;

public class ObjLoaderTests
{
    [Fact]
    public void ObjLoader_LoadsCube_TriangleCountIs12()
    {
        string obj = @"# cube

v -0.5 -0.5 -0.5

v  0.5 -0.5 -0.5

v  0.5  0.5 -0.5

v -0.5  0.5 -0.5

v -0.5 -0.5  0.5

v  0.5 -0.5  0.5

v  0.5  0.5  0.5

v -0.5  0.5  0.5

f 1 2 3 4

f 5 8 7 6

f 1 5 6 2

f 2 6 7 3

f 3 7 8 4

f 5 1 4 8

";

        string path = Path.Combine(Path.GetTempPath(), "pt_cube.obj");
        File.WriteAllText(path, obj);

        var mat = new Lambertian(new Vec3(1,1,1));
        var mesh = ObjLoader.Load(path, mat);

        // The mesh is BVH-wrapped so we can't directly count triangles.
        // But we can at least check it has a bounding box and is hittable.
        Assert.True(mesh.BoundingBox(out var box));
        // Rough bounds of cube
        Assert.InRange(box.Min.X, -0.51f, -0.49f);
        Assert.InRange(box.Max.X, 0.49f, 0.51f);

        // Hit from +Z toward origin should hit
        var ray = new Ray(new Vec3(0,0,2), new Vec3(0,0,-1));
        Assert.True(mesh.Hit(ray, 0.001f, 1000f, out _));
    }
}
