using Core.Camera;
using Core.Math;
using Core.Random;
using Core.Sampling;

namespace Tests.Camera;

public class ShutterTimeTests
{
    private sealed class FixedRng : IRng
    {
        private readonly float _value;
        public FixedRng(float value) => _value = value;
        public uint NextUInt() => 0u;
        public float NextFloat01() => _value;
    }

    [Fact]
    public void Pinhole_SamplesTimeInInterval()
    {
        var cam = new PinholeCamera(
            vfovDegrees: 40f,
            aspectRatio: 1f,
            lookFrom: new Vec3(0, 0, 0),
            lookAt: new Vec3(0, 0, -1),
            vUp: Vec3.UnitY,
            shutterOpen: 2f,
            shutterClose: 6f);

        var sampler = new Sampler(new FixedRng(0.25f));
        var r = cam.GetRay(0.5f, 0.5f, sampler);

        // time = lerp(2,6,0.25) = 3
        Assert.InRange(r.Time, 2.999f, 3.001f);
    }
}