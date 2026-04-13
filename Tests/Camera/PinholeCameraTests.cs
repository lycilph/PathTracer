using Core.Camera;
using Core.Math;

namespace Tests.Camera;

public class PinholeCameraTests
{
    [Fact]
    public void GetRay_IsDeterministic()
    {
        var cam = new PinholeCamera(
            vfovDegrees: 90f,
            aspectRatio: 1f,
            lookFrom: new Vec3(0, 0, 0),
            lookAt: new Vec3(0, 0, -1),
            vUp: Vec3.UnitY);

        var r1 = cam.GetRay(0.5f, 0.5f);
        var r2 = cam.GetRay(0.5f, 0.5f);

        Assert.Equal(r1.Origin, r2.Origin);
        Assert.Equal(r1.Direction, r2.Direction);
        Assert.Equal(r1.Time, r2.Time);
    }

    [Fact]
    public void CenterRay_PointsRoughlyForward()
    {
        var cam = new PinholeCamera(
            vfovDegrees: 90f,
            aspectRatio: 1f,
            lookFrom: new Vec3(0, 0, 0),
            lookAt: new Vec3(0, 0, -1),
            vUp: Vec3.UnitY);

        var r = cam.GetRay(0.5f, 0.5f);
        var d = r.Direction.Normalized();

        // Should point mostly in -Z
        Assert.True(d.Z < -0.7f);
    }
}