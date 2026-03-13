using FluentAssertions;

namespace Core.Tests;

public class QuadTests
{
    // A 2×2 quad in the XY plane centred at origin, facing +Z
    // Corner at (-1,-1,0), edges along +X and +Y
    private static readonly Quad XyQuad = new(
        Corner: new Vector3(-1, -1, 0),
        Edge1: new Vector3(2, 0, 0),
        Edge2: new Vector3(0, 2, 0));

    [Fact]
    public void Hit_RayThroughCentre_ReturnsHit()
    {
        var ray = new Ray(new Vector3(0, 0, -5), Vector3.UnitZ);
        XyQuad.Hit(ray, out var hit).Should().BeTrue();
        hit.T.Should().BeApproximately(5.0, 1e-10);
    }

    [Fact]
    public void Hit_RayThroughCorner_ReturnsHit()
    {
        // Aim exactly at the corner (-1,-1,0)
        var target = new Vector3(-1, -1, 0);
        var origin = new Vector3(-1, -1, -5);
        var ray = new Ray(origin, Vector3.UnitZ);
        XyQuad.Hit(ray, out _).Should().BeTrue();
    }

    [Fact]
    public void Hit_RayOutsideEdge_ReturnsFalse()
    {
        // Ray aimed at (2,0,0) — outside the quad boundary
        var ray = new Ray(new Vector3(2, 0, -5), Vector3.UnitZ);
        XyQuad.Hit(ray, out _).Should().BeFalse();
    }

    [Fact]
    public void Hit_ParallelRay_ReturnsFalse()
    {
        // Ray travelling in the plane of the quad
        var ray = new Ray(new Vector3(0, 0, 0), Vector3.UnitX);
        XyQuad.Hit(ray, out _).Should().BeFalse();
    }

    [Fact]
    public void Hit_RayFromBehind_ReturnsFalse()
    {
        // Ray starts at Z=+5 pointing away from the quad
        var ray = new Ray(new Vector3(0, 0, 5), Vector3.UnitZ);
        XyQuad.Hit(ray, out _).Should().BeFalse();
    }

    [Fact]
    public void Hit_NormalOposesRay()
    {
        var ray = new Ray(new Vector3(0, 0, -5), Vector3.UnitZ);
        XyQuad.Hit(ray, out var hit);
        Vector3.Dot(ray.Direction, hit.Normal).Should().BeLessThan(0);
    }

    [Fact]
    public void Hit_RespectsRayTMax()
    {
        // Quad is at t=5 but TMax is 3 — should miss
        var ray = new Ray(new Vector3(0, 0, -5), Vector3.UnitZ, TMax: 3.0);
        XyQuad.Hit(ray, out _).Should().BeFalse();
    }

    [Fact]
    public void Normal_IsUnitLength()
    {
        XyQuad.Normal.Length.Should().BeApproximately(1.0, 1e-10);
    }
}