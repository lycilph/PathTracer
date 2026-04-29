using Core.Camera;
using Core.Math;
using Core.Random;
using Core.Sampling;

namespace Tests.Camera;

public class ThinLensCameraTests
{
    [Fact]
    public void RaysConvergeOnFocusPlane_ForSamePixel()
    {
        //int w = 100, h = 100;
        float aspect = 1f;

        var lookFrom = new Vec3(0, 0, 0);
        var lookAt = new Vec3(0, 0, -1);
        var vUp = Vec3.UnitY;

        float focusDist = 10f;
        float aperture = 0.5f;

        var pinhole = new PinholeCamera(40f, aspect, lookFrom, lookAt, vUp);
        var thin = new ThinLensCamera(40f, aspect, lookFrom, lookAt, vUp, focusDist, aperture);

        // Choose a pixel sample location
        float u = 0.37f;
        float v = 0.61f;

        // Expected focus point from pinhole ray intersecting focus plane
        var forward = (lookAt - lookFrom).Normalized();
        var pinRay = pinhole.GetRay(u, v, new Sampler(new Pcg32(1)));
        float denom = Vec3.Dot(pinRay.Direction, forward);
        float tFocus = focusDist / denom;
        var expected = lookFrom + pinRay.Direction * tFocus;

        // Two different samplers => different lens points
        var r1 = thin.GetRay(u, v, new Sampler(new Pcg32(10)));
        var r2 = thin.GetRay(u, v, new Sampler(new Pcg32(20)));

        // Intersect each ray with focus plane (point = origin + forward*focusDist, normal = forward)
        var p0 = lookFrom + forward * focusDist;

        Vec3 HitPlane(Ray r)
        {
            float t = Vec3.Dot(p0 - r.Origin, forward) / Vec3.Dot(r.Direction, forward);
            return r.At(t);
        }

        var a = HitPlane(r1);
        var b = HitPlane(r2);

        // Both should be (very close to) expected
        Assert.InRange((a - expected).Length(), 0f, 1e-3f);
        Assert.InRange((b - expected).Length(), 0f, 1e-3f);
    }
}