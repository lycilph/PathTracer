using Core.Materials;
using FluentAssertions;

namespace Core.Tests;

public class TriangleTests
{
    private static readonly IMaterial DummyMat = new Lambertian(Vector3.One);

    // A simple triangle in the XY plane facing +Z
    private static Triangle MakeXyTriangle() => new(
        new Vector3(0, 0, 0),
        new Vector3(1, 0, 0),
        new Vector3(0, 1, 0),
        DummyMat);

    // ── Basic intersection ────────────────────────────────────────────────────

    [Fact]
    public void Hit_RayThroughCentroid_ReturnsHit()
    {
        var tri = MakeXyTriangle();
        // Centroid is at (1/3, 1/3, 0) — shoot from behind
        var ray = new Ray(new Vector3(1.0 / 3, 1.0 / 3, -5), Vector3.UnitZ);
        tri.Hit(ray, out var hit).Should().BeTrue();
        hit.T.Should().BeApproximately(5.0, 1e-10);
    }

    [Fact]
    public void Hit_RayMissesOutsideEdge_ReturnsFalse()
    {
        var tri = MakeXyTriangle();
        // Ray aimed at (0.6, 0.6, 0) — outside the triangle (u+v > 1)
        var ray = new Ray(new Vector3(0.6, 0.6, -5), Vector3.UnitZ);
        tri.Hit(ray, out _).Should().BeFalse();
    }

    [Fact]
    public void Hit_RayParallelToTriangle_ReturnsFalse()
    {
        var tri = MakeXyTriangle();
        var ray = new Ray(new Vector3(0.2, 0.2, 0), Vector3.UnitX);
        tri.Hit(ray, out _).Should().BeFalse();
    }

    [Fact]
    public void Hit_RayFromBehind_ReturnsFalse()
    {
        var tri = MakeXyTriangle();
        var ray = new Ray(new Vector3(0.2, 0.2, 5), Vector3.UnitZ);
        tri.Hit(ray, out _).Should().BeFalse();
    }

    [Fact]
    public void Hit_RayAtVertex_ReturnsHit()
    {
        var tri = MakeXyTriangle();
        var ray = new Ray(new Vector3(0, 0, -5), Vector3.UnitZ);
        tri.Hit(ray, out _).Should().BeTrue();
    }

    [Fact]
    public void Hit_RespectsRayTMax()
    {
        var tri = MakeXyTriangle();
        var ray = new Ray(new Vector3(0.2, 0.2, -5), Vector3.UnitZ, TMax: 3.0);
        tri.Hit(ray, out _).Should().BeFalse();
    }

    // ── Normals ───────────────────────────────────────────────────────────────

    [Fact]
    public void Hit_FlatNormal_FacesCorrectDirection()
    {
        var tri = MakeXyTriangle();
        var ray = new Ray(new Vector3(0.2, 0.2, -5), Vector3.UnitZ);
        tri.Hit(ray, out var hit);

        // The normal always opposes the ray — regardless of winding
        Vector3.Dot(ray.Direction, hit.Normal).Should().BeLessThan(0,
            because: "normal must always oppose the incident ray");
        hit.Normal.Length.Should().BeApproximately(1.0, 1e-10);
    }

    [Fact]
    public void Hit_SmoothNormals_InterpolatesCorrectly()
    {
        // Triangle in XZ plane, ray from above travelling -Y
        // All vertex normals point +Y — interpolated normal should be +Y (or -Y if flipped)
        var tri = new Triangle(
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(0, 0, 1),
            DummyMat,
            n0: Vector3.UnitY,
            n1: Vector3.UnitY,
            n2: Vector3.UnitY);

        var ray = new Ray(new Vector3(0.2, 5, 0.2), -Vector3.UnitY);
        tri.Hit(ray, out var hit).Should().BeTrue();

        // Normal must oppose the ray and be unit length
        Vector3.Dot(ray.Direction, hit.Normal).Should().BeLessThan(0,
            because: "normal must always oppose the incident ray");
        hit.Normal.Length.Should().BeApproximately(1.0, 1e-10);

        // The normal should be axis-aligned along Y (either +Y or -Y after flip)
        hit.Normal.X.Should().BeApproximately(0.0, 1e-10);
        hit.Normal.Z.Should().BeApproximately(0.0, 1e-10);
    }

    [Fact]
    public void Hit_SmoothNormals_VaryAcrossSurface()
    {
        // Vertex normals that differ — interpolated normal should vary
        // depending on which part of the triangle is hit
        var tri = new Triangle(
            new Vector3(0, 0, 0),
            new Vector3(1, 0, 0),
            new Vector3(0, 0, 1),
            DummyMat,
            n0: Vector3.UnitY,
            n1: new Vector3(1, 1, 0).Normalize(),
            n2: new Vector3(0, 1, 1).Normalize());

        // Hit near v1 and near v2 — normals should differ
        var rayNearV1 = new Ray(new Vector3(0.9, 5, 0.05), -Vector3.UnitY);
        var rayNearV2 = new Ray(new Vector3(0.05, 5, 0.9), -Vector3.UnitY);

        tri.Hit(rayNearV1, out var hitV1);
        tri.Hit(rayNearV2, out var hitV2);

        hitV1.Normal.Should().NotBe(hitV2.Normal,
            because: "smooth normals must vary across the surface");
    }

    // ── Bounds ────────────────────────────────────────────────────────────────

    [Fact]
    public void GetBounds_ContainsAllVertices()
    {
        var tri = MakeXyTriangle();
        var bounds = tri.GetBounds();

        bounds.Min.X.Should().BeLessThanOrEqualTo(0);
        bounds.Min.Y.Should().BeLessThanOrEqualTo(0);
        bounds.Max.X.Should().BeGreaterThanOrEqualTo(1);
        bounds.Max.Y.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void GetBounds_HasPositiveThickness()
    {
        // Even a flat triangle must have nonzero thickness due to padding
        var tri = MakeXyTriangle();
        var bounds = tri.GetBounds();

        (bounds.Max.Z - bounds.Min.Z).Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetBounds_RayHittingTriangleAlsoHitsBounds()
    {
        var tri = MakeXyTriangle();
        var bounds = tri.GetBounds();
        var ray = new Ray(new Vector3(0.2, 0.2, -5), Vector3.UnitZ);

        tri.Hit(ray, out _).Should().BeTrue();
        bounds.Hit(ray).Should().BeTrue();
    }
}