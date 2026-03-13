using FluentAssertions;

namespace Core.Tests;

public class SphereTests
{
    // Sphere at origin, radius 1 — a convenient test fixture
    private static readonly Sphere UnitSphere = new(Vector3.Zero, 1.0);

    [Fact]
    public void Hit_RayStraightAtSphere_ReturnsHit()
    {
        var ray = new Ray(new Vector3(0, 0, -5), Vector3.UnitZ);
        UnitSphere.Hit(ray, out var hit).Should().BeTrue();
        hit.T.Should().BeApproximately(4.0, 1e-10);
    }

    [Fact]
    public void Hit_RayMissesSphere_ReturnsFalse()
    {
        // Ray travelling along X, well above the sphere
        var ray = new Ray(new Vector3(0, 2, 0), Vector3.UnitX);
        UnitSphere.Hit(ray, out _).Should().BeFalse();
    }

    [Fact]
    public void Hit_RayOriginInsideSphere_HitsBackWall()
    {
        // Ray starts at origin (inside the unit sphere), travels +Z
        // Should hit the far side at t=1
        var ray = new Ray(Vector3.Zero, Vector3.UnitZ);
        UnitSphere.Hit(ray, out var hit).Should().BeTrue();
        hit.T.Should().BeApproximately(1.0, 1e-10);
        hit.FrontFace.Should().BeFalse(); // hitting the inside
    }

    [Fact]
    public void Hit_RayBehindSphere_ReturnsFalse()
    {
        // Sphere is at origin; ray starts at Z=+5 pointing away (+Z)
        var ray = new Ray(new Vector3(0, 0, 5), Vector3.UnitZ);
        UnitSphere.Hit(ray, out _).Should().BeFalse();
    }

    [Fact]
    public void Hit_NormalPointsOutward_WhenHittingOutside()
    {
        // Ray from -Z hitting front of sphere — normal should point toward camera (-Z)
        var ray = new Ray(new Vector3(0, 0, -5), Vector3.UnitZ);
        UnitSphere.Hit(ray, out var hit);

        // The outward normal at the front of the sphere is -UnitZ,
        // and since we hit the front face it should be preserved
        hit.Normal.Should().Be(-Vector3.UnitZ);
        hit.FrontFace.Should().BeTrue();
    }

    [Fact]
    public void Hit_TangentRay_CountsAsHit()
    {
        // Ray grazing the very top of the sphere (y=1)
        var ray = new Ray(new Vector3(0, 1, -5), Vector3.UnitZ);
        UnitSphere.Hit(ray, out _).Should().BeTrue();
    }

    [Fact]
    public void Hit_RespectsRayTMax()
    {
        // Sphere is at t=4 but we cap TMax at 3 — should miss
        var ray = new Ray(new Vector3(0, 0, -5), Vector3.UnitZ, TMax: 3.0);
        UnitSphere.Hit(ray, out _).Should().BeFalse();
    }
}