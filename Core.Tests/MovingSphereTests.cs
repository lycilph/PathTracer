using Core.Materials;
using FluentAssertions;

namespace Core.Tests;

public class MovingSphereTests
{
    private static readonly IMaterial DummyMat = new Lambertian(Vector3.One);

    [Fact]
    public void CentreAt_Time0_ReturnsStartCentre()
    {
        var sphere = new MovingSphere(
            Vector3.Zero, new Vector3(1, 0, 0),
            0.0, 1.0, 0.5, DummyMat);

        sphere.CentreAt(0.0).Should().Be(Vector3.Zero);
    }

    [Fact]
    public void CentreAt_Time1_ReturnsEndCentre()
    {
        var sphere = new MovingSphere(
            Vector3.Zero, new Vector3(1, 0, 0),
            0.0, 1.0, 0.5, DummyMat);

        sphere.CentreAt(1.0).X.Should().BeApproximately(1.0, 1e-10);
    }

    [Fact]
    public void CentreAt_MidTime_ReturnsMidpoint()
    {
        var sphere = new MovingSphere(
            Vector3.Zero, new Vector3(2, 0, 0),
            0.0, 1.0, 0.5, DummyMat);

        sphere.CentreAt(0.5).X.Should().BeApproximately(1.0, 1e-10);
    }

    [Fact]
    public void Hit_RayAtTime0_HitsSphereAtStartPosition()
    {
        var sphere = new MovingSphere(
            new Vector3(0, 0, 5), new Vector3(10, 0, 5),
            0.0, 1.0, 1.0, DummyMat);

        // Ray at time=0 should hit sphere at start position (0,0,5)
        var ray = new Ray(Vector3.Zero, Vector3.UnitZ, Time: 0.0);
        sphere.Hit(ray, out var hit).Should().BeTrue();
        hit.T.Should().BeApproximately(4.0, 0.1);
    }

    [Fact]
    public void Hit_RayAtTime1_HitsSphereAtEndPosition()
    {
        var sphere = new MovingSphere(
            new Vector3(0, 0, 5), new Vector3(0, 0, 10),
            0.0, 1.0, 1.0, DummyMat);

        // Ray at time=1 should hit sphere at end position (0,0,10)
        var ray = new Ray(Vector3.Zero, Vector3.UnitZ, Time: 1.0);
        sphere.Hit(ray, out var hit).Should().BeTrue();
        hit.T.Should().BeApproximately(9.0, 0.1);
    }

    [Fact]
    public void Hit_RayAtTime0_MissesSphereAtTime1Position()
    {
        // Sphere starts off-axis, moves on-axis — ray at time=0 should miss
        var sphere = new MovingSphere(
            new Vector3(5, 0, 5), new Vector3(0, 0, 5),
            0.0, 1.0, 0.4, DummyMat);

        var ray = new Ray(Vector3.Zero, Vector3.UnitZ, Time: 0.0);
        sphere.Hit(ray, out _).Should().BeFalse();
    }

    [Fact]
    public void GetBounds_EnclosesEntireMotionPath()
    {
        var sphere = new MovingSphere(
            new Vector3(0, 0, 0), new Vector3(4, 0, 0),
            0.0, 1.0, 1.0, DummyMat);

        var bounds = sphere.GetBounds();

        // Bounds must cover both endpoints plus radius
        bounds.Min.X.Should().BeLessThanOrEqualTo(-1);
        bounds.Max.X.Should().BeGreaterThanOrEqualTo(5);
    }
}