using Core.Algebra;
using FluentAssertions;

namespace Core.Tests.Algebra;

public class Matrix4x4dTests
{
    [Fact]
    public void Identity_TransformPoint_IsUnchanged()
    {
        var p = new Vector3(1, 2, 3);
        var result = Matrix4x4d.Identity.TransformPoint(p);
        result.Should().Be(p);
    }

    [Fact]
    public void Translation_MovesPoint()
    {
        var m = Matrix4x4d.Translation(1, 2, 3);
        var result = m.TransformPoint(Vector3.Zero);
        result.Should().Be(new Vector3(1, 2, 3));
    }

    [Fact]
    public void Translation_DoesNotAffectDirection()
    {
        var m = Matrix4x4d.Translation(10, 20, 30);
        var result = m.TransformDirection(Vector3.UnitX);
        result.Should().Be(Vector3.UnitX);
    }

    [Fact]
    public void Scale_ScalesPoint()
    {
        var m = Matrix4x4d.Scale(2);
        var result = m.TransformPoint(new Vector3(1, 2, 3));
        result.X.Should().BeApproximately(2, 1e-10);
        result.Y.Should().BeApproximately(4, 1e-10);
        result.Z.Should().BeApproximately(6, 1e-10);
    }

    [Fact]
    public void RotationY_90Degrees_RotatesXToNegZ()
    {
        var m = Matrix4x4d.RotationY(90);
        var result = m.TransformDirection(Vector3.UnitX);
        result.X.Should().BeApproximately(0, 1e-10);
        result.Y.Should().BeApproximately(0, 1e-10);
        result.Z.Should().BeApproximately(-1, 1e-10);
    }

    [Fact]
    public void Multiply_TranslationThenScale_IsCorrect()
    {
        // Scale first then translate: point (1,0,0) scaled by 2 = (2,0,0)
        // then translated by (1,0,0) = (3,0,0)
        var m = Matrix4x4d.Translation(1, 0, 0) * Matrix4x4d.Scale(2);
        var result = m.TransformPoint(new Vector3(1, 0, 0));
        result.X.Should().BeApproximately(3, 1e-10);
    }

    [Fact]
    public void Inverse_OfTranslation_IsCorrect()
    {
        var m = Matrix4x4d.Translation(3, -1, 2);
        var inv = m.Inverse();
        var p = new Vector3(5, 5, 5);

        // M * M⁻¹ * p should equal p
        var result = m.TransformPoint(inv.TransformPoint(p));
        result.X.Should().BeApproximately(p.X, 1e-10);
        result.Y.Should().BeApproximately(p.Y, 1e-10);
        result.Z.Should().BeApproximately(p.Z, 1e-10);
    }

    [Fact]
    public void Inverse_OfScale_IsCorrect()
    {
        var m = Matrix4x4d.Scale(4);
        var inv = m.Inverse();
        var p = new Vector3(8, 4, 2);

        var result = inv.TransformPoint(p);
        result.X.Should().BeApproximately(2, 1e-10);
        result.Y.Should().BeApproximately(1, 1e-10);
        result.Z.Should().BeApproximately(0.5, 1e-10);
    }

    [Fact]
    public void MultiplyByInverse_ProducesIdentity()
    {
        var m = Matrix4x4d.Translation(1, 2, 3) *
                  Matrix4x4d.RotationY(45) *
                  Matrix4x4d.Scale(2);
        var inv = m.Inverse();

        // M * M⁻¹ should be identity — test on several points
        foreach (var p in new[] {
            new Vector3(1, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(3, -2, 1) })
        {
            var result = m.TransformPoint(inv.TransformPoint(p));
            result.X.Should().BeApproximately(p.X, 1e-8);
            result.Y.Should().BeApproximately(p.Y, 1e-8);
            result.Z.Should().BeApproximately(p.Z, 1e-8);
        }
    }
}