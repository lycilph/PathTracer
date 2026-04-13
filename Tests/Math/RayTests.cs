using Core.Math;

namespace Tests.Math;

public class RayTests
{
    private static void AssertNear(float expected, float actual, float eps = 1e-6f)
        => Assert.True(float.Abs(expected - actual) <= eps, $"Expected {expected} but got {actual}");

    [Fact]
    public void At_ComputesPointAlongRay()
    {
        var r = new Ray(new Vec3(1, 2, 3), new Vec3(0, 0, -2));
        var p = r.At(1.5f);

        AssertNear(1f, p.X);
        AssertNear(2f, p.Y);
        AssertNear(0f, p.Z);
    }

    [Fact]
    public void Time_IsStored()
    {
        var r = new Ray(Vec3.Zero, Vec3.UnitZ, time: 0.25f);
        AssertNear(0.25f, r.Time);
    }
}
