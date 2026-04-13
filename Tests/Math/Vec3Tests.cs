using Core.Math;

namespace Tests.Math;

public class Vec3Tests
{
    private static void AssertNear(float expected, float actual, float eps = 1e-6f)
        => Assert.True(float.Abs(expected - actual) <= eps, $"Expected {expected} but got {actual}");

    [Fact]
    public void Add_Subtract_Works()
    {
        var a = new Vec3(1, 2, 3);
        var b = new Vec3(4, -5, 6);

        var c = a + b;
        Assert.Equal(new Vec3(5, -3, 9), c);

        var d = c - a;
        Assert.Equal(b, d);
    }

    [Fact]
    public void DotProduct_KnownValues()
    {
        var a = new Vec3(1, 2, 3);
        var b = new Vec3(4, -5, 6);

        // 1*4 + 2*(-5) + 3*6 = 4 -10 + 18 = 12
        AssertNear(12f, Vec3.Dot(a, b));
    }

    [Fact]
    public void CrossProduct_RightHandRule()
    {
        var z = Vec3.Cross(Vec3.UnitX, Vec3.UnitY);
        AssertNear(0f, z.X);
        AssertNear(0f, z.Y);
        AssertNear(1f, z.Z);
    }

    [Fact]
    public void Normalize_ZeroVector_ReturnsZero()
    {
        var n = Vec3.Zero.Normalized();
        Assert.True(n.NearZero());
    }

    [Fact]
    public void Normalize_NonZero_HasLengthOne()
    {
        var v = new Vec3(3, 0, 4);
        var n = v.Normalized();
        AssertNear(1f, n.Length(), 1e-6f);
    }
}
