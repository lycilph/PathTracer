using Core.Acceleration;
using Core.Algebra;
using FluentAssertions;

namespace Core.Tests.Acceleration;

public class AabbTests
{
    // Unit box from (0,0,0) to (1,1,1)
    private static readonly Aabb UnitBox = new(Vector3.Zero, Vector3.One);

    [Fact]
    public void Hit_RayStraightThrough_ReturnsTrue()
    {
        var ray = new Ray(new Vector3(0.5, 0.5, -1), Vector3.UnitZ);
        UnitBox.Hit(ray).Should().BeTrue();
    }

    [Fact]
    public void Hit_RayMissesCompletely_ReturnsFalse()
    {
        var ray = new Ray(new Vector3(2, 2, -1), Vector3.UnitZ);
        UnitBox.Hit(ray).Should().BeFalse();
    }

    [Fact]
    public void Hit_RayOriginInsideBox_ReturnsTrue()
    {
        var ray = new Ray(new Vector3(0.5, 0.5, 0.5), Vector3.UnitZ);
        UnitBox.Hit(ray).Should().BeTrue();
    }

    [Fact]
    public void Hit_RayParallelToSlabOutside_ReturnsFalse()
    {
        // Ray travelling along Z but outside the X slab
        var ray = new Ray(new Vector3(2, 0.5, -1), Vector3.UnitZ);
        UnitBox.Hit(ray).Should().BeFalse();
    }

    [Fact]
    public void Hit_RayParallelToSlabInside_ReturnsTrue()
    {
        // Ray travelling along Z, inside all slabs
        var ray = new Ray(new Vector3(0.5, 0.5, -1), Vector3.UnitZ);
        UnitBox.Hit(ray).Should().BeTrue();
    }

    [Fact]
    public void Hit_RespectsRayTMax()
    {
        // Box starts at z=0, ray starts at z=-5, so hit is at t=5
        // TMax of 3 should miss
        var ray = new Ray(new Vector3(0.5, 0.5, -5), Vector3.UnitZ, TMax: 3.0);
        UnitBox.Hit(ray).Should().BeFalse();
    }

    [Fact]
    public void ExpandTo_ProducesCorrectUnion()
    {
        var a = new Aabb(Vector3.Zero, Vector3.One);
        var b = new Aabb(new Vector3(1, 1, 1), new Vector3(2, 2, 2));
        var union = a.ExpandTo(b);

        union.Min.Should().Be(Vector3.Zero);
        union.Max.Should().Be(new Vector3(2, 2, 2));
    }

    [Fact]
    public void Centroid_IsCorrect()
    {
        var box = new Aabb(Vector3.Zero, new Vector3(2, 4, 6));
        box.Centroid.Should().Be(new Vector3(1, 2, 3));
    }

    [Fact]
    public void SurfaceArea_UnitBox_IsSix()
    {
        // Unit cube has 6 faces each of area 1
        UnitBox.SurfaceArea.Should().BeApproximately(6.0, 1e-10);
    }
}