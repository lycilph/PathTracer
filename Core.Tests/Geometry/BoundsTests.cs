using Core.Algebra;
using Core.Geometry;
using FluentAssertions;

namespace Core.Tests.Geometry;

public class BoundsTests
{
    private static readonly IMaterial DummyMat = new TestMaterial();

    // ── Sphere bounds ─────────────────────────────────────────────────────────

    [Fact]
    public void Sphere_Bounds_ContainsCentre()
    {
        var sphere = new Sphere(new Vector3(1, 2, 3), 0.5, DummyMat);
        var bounds = sphere.GetBounds();

        bounds.Min.X.Should().BeLessThanOrEqualTo(1.0);
        bounds.Min.Y.Should().BeLessThanOrEqualTo(2.0);
        bounds.Min.Z.Should().BeLessThanOrEqualTo(3.0);
        bounds.Max.X.Should().BeGreaterThanOrEqualTo(1.0);
        bounds.Max.Y.Should().BeGreaterThanOrEqualTo(2.0);
        bounds.Max.Z.Should().BeGreaterThanOrEqualTo(3.0);
    }

    [Fact]
    public void Sphere_Bounds_IsCorrectSize()
    {
        var sphere = new Sphere(Vector3.Zero, 1.0, DummyMat);
        var bounds = sphere.GetBounds();

        bounds.Min.Should().Be(new Vector3(-1, -1, -1));
        bounds.Max.Should().Be(new Vector3(1, 1, 1));
    }

    [Fact]
    public void Sphere_Bounds_RayHittingSphereAlsoHitsBounds()
    {
        // Any ray that hits the sphere must also hit its bounding box
        var sphere = new Sphere(new Vector3(0, 0, 5), 1.0, DummyMat);
        var bounds = sphere.GetBounds();
        var ray = new Ray(Vector3.Zero, Vector3.UnitZ);

        sphere.Hit(ray, out _).Should().BeTrue();
        bounds.Hit(ray).Should().BeTrue();
    }

    // ── Quad bounds ───────────────────────────────────────────────────────────

    [Fact]
    public void Quad_Bounds_ContainsAllCorners()
    {
        var quad = new Quad(
            new Vector3(-1, 0, -1),
            new Vector3(2, 0, 0),
            new Vector3(0, 0, 2), DummyMat);
        var bounds = quad.GetBounds();

        // All four corners must be inside the bounds
        bounds.Min.X.Should().BeLessThanOrEqualTo(-1);
        bounds.Min.Z.Should().BeLessThanOrEqualTo(-1);
        bounds.Max.X.Should().BeGreaterThanOrEqualTo(1);
        bounds.Max.Z.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void Quad_Bounds_HasPositiveThickness()
    {
        // A flat XZ quad should still have nonzero Y thickness due to padding
        var quad = new Quad(
            new Vector3(-1, 0, -1),
            new Vector3(2, 0, 0),
            new Vector3(0, 0, 2), DummyMat);
        var bounds = quad.GetBounds();

        (bounds.Max.Y - bounds.Min.Y).Should().BeGreaterThan(0,
            because: "flat quads need padding to avoid zero-thickness AABB");
    }

    [Fact]
    public void Quad_Bounds_RayHittingQuadAlsoHitsBounds()
    {
        var quad = new Quad(
            new Vector3(-1, -1, 0),
            new Vector3(2, 0, 0),
            new Vector3(0, 2, 0), DummyMat);
        var bounds = quad.GetBounds();
        var ray = new Ray(new Vector3(0, 0, -5), Vector3.UnitZ);

        quad.Hit(ray, out _).Should().BeTrue();
        bounds.Hit(ray).Should().BeTrue();
    }
}