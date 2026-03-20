using Core.Algebra;
using FluentAssertions;

namespace Core.Tests.Algebra;

public class RayTests
{
    [Fact]
    public void At_ZeroT_ReturnsOrigin()
    {
        var ray = new Ray(new Vector3(1, 2, 3), Vector3.UnitX);
        ray.At(0).Should().Be(new Vector3(1, 2, 3));
    }

    [Fact]
    public void At_UnitDirection_TEqualsDistance()
    {
        // Moving 5 units along X from origin (2,0,0) should reach (7,0,0)
        var ray = new Ray(new Vector3(2, 0, 0), Vector3.UnitX);
        ray.At(5).Should().Be(new Vector3(7, 0, 0));
    }

    [Fact]
    public void At_DiagonalDirection_IsCorrect()
    {
        var dir = new Vector3(1, 1, 0).Normalize();
        var ray = new Ray(Vector3.Zero, dir);

        var point = ray.At(Math.Sqrt(2));

        // Should land near (1,1,0)
        point.X.Should().BeApproximately(1.0, 1e-10);
        point.Y.Should().BeApproximately(1.0, 1e-10);
        point.Z.Should().BeApproximately(0.0, 1e-10);
    }

    [Fact]
    public void DefaultTMin_IsSmallPositive()
    {
        var ray = new Ray(Vector3.Zero, Vector3.UnitZ);
        ray.TMin.Should().Be(1e-4);
    }

    [Fact]
    public void DefaultTMax_IsInfinity()
    {
        var ray = new Ray(Vector3.Zero, Vector3.UnitZ);
        ray.TMax.Should().Be(double.PositiveInfinity);
    }
}