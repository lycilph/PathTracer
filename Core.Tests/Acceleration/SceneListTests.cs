using Core.Acceleration;
using Core.Algebra;
using Core.Geometry;
using FluentAssertions;

namespace Core.Tests.Acceleration;

public class SceneListTests
{
    // At the top of each test class, add a shared dummy material:
    private static readonly IMaterial DummyMat = new TestMaterial();

    [Fact]
    public void Hit_EmptyScene_ReturnsFalse()
    {
        var scene = new SceneList();
        var ray = new Ray(Vector3.Zero, Vector3.UnitZ);
        scene.Hit(ray, out _).Should().BeFalse();
    }

    [Fact]
    public void Hit_SingleSphere_ReturnsHit()
    {
        var scene = new SceneList();
        scene.Add(new Sphere(new Vector3(0, 0, 5), 1.0, DummyMat));

        var ray = new Ray(Vector3.Zero, Vector3.UnitZ);
        scene.Hit(ray, out var hit).Should().BeTrue();
        hit.T.Should().BeApproximately(4.0, 1e-10);
    }

    [Fact]
    public void Hit_TwoSpheresInLine_ReturnsNearest()
    {
        // Two spheres along +Z — the closer one should win
        var scene = new SceneList();
        scene.Add(new Sphere(new Vector3(0, 0, 10), 1.0, DummyMat)); // far, t≈9
        scene.Add(new Sphere(new Vector3(0, 0, 5), 1.0, DummyMat)); // near, t≈4

        var ray = new Ray(Vector3.Zero, Vector3.UnitZ);
        scene.Hit(ray, out var hit).Should().BeTrue();
        hit.T.Should().BeApproximately(4.0, 1e-10);
    }

    [Fact]
    public void Hit_TwoSpheresInLine_OrderIndependent()
    {
        // Same as above but added in the opposite order — result must be identical
        var scene = new SceneList();
        scene.Add(new Sphere(new Vector3(0, 0, 5), 1.0, DummyMat)); // near
        scene.Add(new Sphere(new Vector3(0, 0, 10), 1.0, DummyMat)); // far

        var ray = new Ray(Vector3.Zero, Vector3.UnitZ);
        scene.Hit(ray, out var hit).Should().BeTrue();
        hit.T.Should().BeApproximately(4.0, 1e-10);
    }

    [Fact]
    public void Hit_RayMissesAllPrimitives_ReturnsFalse()
    {
        var scene = new SceneList();
        scene.Add(new Sphere(new Vector3(0, 0, 5), 1.0, DummyMat));
        scene.Add(new Sphere(new Vector3(0, 0, 10), 1.0, DummyMat));

        // Ray going sideways — misses both spheres
        var ray = new Ray(Vector3.Zero, Vector3.UnitX);
        scene.Hit(ray, out _).Should().BeFalse();
    }

    [Fact]
    public void Hit_RespectsRayTMax()
    {
        var scene = new SceneList();
        scene.Add(new Sphere(new Vector3(0, 0, 5), 1.0, DummyMat)); // hit at t≈4

        // TMax of 3 means the sphere is out of range
        var ray = new Ray(Vector3.Zero, Vector3.UnitZ, TMax: 3.0);
        scene.Hit(ray, out _).Should().BeFalse();
    }
}