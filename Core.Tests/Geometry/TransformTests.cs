using Core.Algebra;
using Core.Geometry;
using FluentAssertions;

namespace Core.Tests.Geometry;

public class TransformTests
{
    private static readonly IMaterial DummyMat = new TestMaterial();

    // Unit sphere at origin
    private static Sphere UnitSphere() => new(Vector3.Zero, 1.0, DummyMat);

    [Fact]
    public void Hit_TranslatedSphere_HitsAtCorrectPosition()
    {
        // Move the sphere to Z=5 via transform
        var m = Matrix4x4d.Translation(0, 0, 5);
        var transform = new Transform(UnitSphere(), m);
        var ray = new Ray(Vector3.Zero, Vector3.UnitZ);

        transform.Hit(ray, out var hit).Should().BeTrue();
        hit.T.Should().BeApproximately(4.0, 1e-6);
    }

    [Fact]
    public void Hit_IdentityTransform_MatchesOriginal()
    {
        var sphere = UnitSphere();
        var transform = new Transform(sphere, Matrix4x4d.Identity);
        var ray = new Ray(new Vector3(0, 0, -3), Vector3.UnitZ);

        var sphereHit = sphere.Hit(ray, out var sphereRecord);
        var transformHit = transform.Hit(ray, out var transformRecord);

        sphereHit.Should().Be(transformHit);
        if (sphereHit)
            transformRecord.T.Should().BeApproximately(sphereRecord.T, 1e-6);
    }

    [Fact]
    public void Hit_ScaledSphere_HitsAtCorrectDistance()
    {
        // Scale sphere by 2 — radius becomes 2, hit at t=1 from Z=-3
        var m = Matrix4x4d.Scale(2);
        var transform = new Transform(UnitSphere(), m);
        var ray = new Ray(new Vector3(0, 0, -3), Vector3.UnitZ);

        transform.Hit(ray, out var hit).Should().BeTrue();
        hit.T.Should().BeApproximately(1.0, 1e-6);
    }

    [Fact]
    public void Hit_TranslatedSphere_MissAfterTranslation()
    {
        // Sphere moved far to the side — ray misses
        var m = Matrix4x4d.Translation(10, 0, 0);
        var transform = new Transform(UnitSphere(), m);
        var ray = new Ray(Vector3.Zero, Vector3.UnitZ);

        transform.Hit(ray, out _).Should().BeFalse();
    }

    [Fact]
    public void Hit_NormalIsTransformedCorrectly()
    {
        // A sphere translated to (0,0,5) — normal at front should point -Z
        var m = Matrix4x4d.Translation(0, 0, 5);
        var transform = new Transform(UnitSphere(), m);
        var ray = new Ray(Vector3.Zero, Vector3.UnitZ);

        transform.Hit(ray, out var hit);

        hit.Normal.X.Should().BeApproximately(0, 1e-6);
        hit.Normal.Y.Should().BeApproximately(0, 1e-6);
        hit.Normal.Z.Should().BeApproximately(-1, 1e-6);
    }

    [Fact]
    public void GetBounds_TranslatedSphere_BoundsAreShifted()
    {
        var m = Matrix4x4d.Translation(5, 0, 0);
        var transform = new Transform(UnitSphere(), m);
        var bounds = transform.GetBounds();

        bounds.Min.X.Should().BeApproximately(4, 1e-6);
        bounds.Max.X.Should().BeApproximately(6, 1e-6);
    }

    [Fact]
    public void Hit_RotatedMesh_HitsCorrectly()
    {
        // A flat quad in the XY plane, rotated 90° around X becomes XZ plane
        var quad = new Quad(
            new Vector3(-1, -1, 0),
            new Vector3(2, 0, 0),
            new Vector3(0, 2, 0),
            DummyMat);

        var rotated = new Transform(quad, Matrix4x4d.RotationX(90));

        // Ray from above (-Y direction) should now hit it
        var ray = new Ray(new Vector3(0, 5, 0), -Vector3.UnitY);
        rotated.Hit(ray, out _).Should().BeTrue();
    }
}