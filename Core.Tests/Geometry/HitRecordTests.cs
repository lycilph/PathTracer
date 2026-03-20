using Core.Algebra;
using Core.Geometry;
using FluentAssertions;

namespace Core.Tests.Geometry;

public class HitRecordTests
{
    // At the top of each test class, add a shared dummy material:
    private static readonly IMaterial DummyMat = new TestMaterial();

    [Fact]
    public void Create_RayHitsOutside_FrontFaceIsTrue()
    {
        // Ray travelling along +Z hitting a surface whose outward normal is -Z
        // (i.e. ray hits the front face of a wall facing toward the camera)
        var ray = new Ray(new Vector3(0, 0, -5), Vector3.UnitZ);
        var outwardNormal = -Vector3.UnitZ;

        var hit = HitRecord.Create(t: 5, point: Vector3.Zero, ray, outwardNormal, DummyMat);

        hit.FrontFace.Should().BeTrue();
        hit.Normal.Should().Be(-Vector3.UnitZ); // normal opposes the ray
    }

    [Fact]
    public void Create_RayHitsInside_FrontFaceIsFalse()
    {
        // Ray travelling along +Z hitting a surface whose outward normal is +Z
        // (i.e. ray is inside a sphere and hitting the back wall of it)
        var ray = new Ray(new Vector3(0, 0, -1), Vector3.UnitZ);
        var outwardNormal = Vector3.UnitZ;

        var hit = HitRecord.Create(t: 1, point: Vector3.Zero, ray, outwardNormal, DummyMat);

        hit.FrontFace.Should().BeFalse();
        hit.Normal.Should().Be(-Vector3.UnitZ); // normal still opposes the ray
    }

    [Fact]
    public void Create_NormalAlwaysOposesRay()
    {
        // For any hit, Dot(ray.Direction, hit.Normal) must be negative
        var ray = new Ray(new Vector3(0, 0, -5), Vector3.UnitZ);

        var hitFront = HitRecord.Create(5, Vector3.Zero, ray, -Vector3.UnitZ, DummyMat);
        var hitBack = HitRecord.Create(5, Vector3.Zero, ray, Vector3.UnitZ, DummyMat);

        Vector3.Dot(ray.Direction, hitFront.Normal).Should().BeLessThan(0);
        Vector3.Dot(ray.Direction, hitBack.Normal).Should().BeLessThan(0);
    }

    [Fact]
    public void Create_StoresCorrectTAndPoint()
    {
        var ray = new Ray(Vector3.Zero, Vector3.UnitX);
        var point = new Vector3(3, 0, 0);

        var hit = HitRecord.Create(t: 3, point, ray, -Vector3.UnitX, DummyMat);

        hit.T.Should().Be(3);
        hit.Point.Should().Be(point);
    }
}