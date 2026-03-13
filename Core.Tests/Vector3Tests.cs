using FluentAssertions;

namespace Core.Tests;

public class Vector3Tests
{
    // ── Arithmetic ────────────────────────────────────────────────────────────

    [Fact]
    public void Addition_SumsComponents()
    {
        var result = new Vector3(1, 2, 3) + new Vector3(4, 5, 6);
        result.Should().Be(new Vector3(5, 7, 9));
    }

    [Fact]
    public void Subtraction_DiffsComponents()
    {
        var result = new Vector3(4, 5, 6) - new Vector3(1, 2, 3);
        result.Should().Be(new Vector3(3, 3, 3));
    }

    [Fact]
    public void ScalarMultiply_ScalesAllComponents()
    {
        (new Vector3(1, 2, 3) * 2.0).Should().Be(new Vector3(2, 4, 6));
        (2.0 * new Vector3(1, 2, 3)).Should().Be(new Vector3(2, 4, 6));
    }

    [Fact]
    public void Negate_FlipsAllComponents()
    {
        (-new Vector3(1, -2, 3)).Should().Be(new Vector3(-1, 2, -3));
    }

    // ── Dot product ───────────────────────────────────────────────────────────

    [Fact]
    public void Dot_PerpendicularVectors_IsZero()
    {
        Vector3.Dot(Vector3.UnitX, Vector3.UnitY).Should().Be(0.0);
    }

    [Fact]
    public void Dot_ParallelUnitVectors_IsOne()
    {
        Vector3.Dot(Vector3.UnitX, Vector3.UnitX).Should().Be(1.0);
    }

    [Fact]
    public void Dot_AntiParallelUnitVectors_IsMinusOne()
    {
        Vector3.Dot(Vector3.UnitX, -Vector3.UnitX).Should().Be(-1.0);
    }

    // ── Cross product ─────────────────────────────────────────────────────────

    [Fact]
    public void Cross_XcrossY_IsZ()
    {
        // Right-hand rule: X × Y = Z
        Vector3.Cross(Vector3.UnitX, Vector3.UnitY).Should().Be(Vector3.UnitZ);
    }

    [Fact]
    public void Cross_YcrossX_IsNegativeZ()
    {
        Vector3.Cross(Vector3.UnitY, Vector3.UnitX).Should().Be(-Vector3.UnitZ);
    }

    [Fact]
    public void Cross_ParallelVectors_IsZero()
    {
        Vector3.Cross(Vector3.UnitX, Vector3.UnitX).Should().Be(Vector3.Zero);
    }

    // ── Length & normalise ────────────────────────────────────────────────────

    [Fact]
    public void Length_KnownVector_IsCorrect()
    {
        new Vector3(3, 4, 0).Length.Should().BeApproximately(5.0, 1e-10);
    }

    [Fact]
    public void Normalize_ProducesUnitVector()
    {
        var unit = new Vector3(3, 4, 0).Normalize();
        unit.Length.Should().BeApproximately(1.0, 1e-10);
    }

    [Fact]
    public void Normalize_PreservesDirection()
    {
        var v = new Vector3(0, 7, 0).Normalize();
        v.Should().Be(Vector3.UnitY);
    }

    // ── Edge cases ────────────────────────────────────────────────────────────

    [Fact]
    public void IsNearZero_ZeroVector_IsTrue()
    {
        Vector3.Zero.IsNearZero().Should().BeTrue();
    }

    [Fact]
    public void IsNearZero_NonZeroVector_IsFalse()
    {
        new Vector3(0.001, 0, 0).IsNearZero().Should().BeFalse();
    }
}