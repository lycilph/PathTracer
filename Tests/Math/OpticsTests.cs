using Core.Math;

namespace Tests.Math;

public class OpticsTests
{
    [Fact]
    public void Reflect_MirrorsAroundNormal()
    {
        var n = Vec3.UnitY;
        var v = new Vec3(1, -1, 0).Normalized();
        var r = Optics.Reflect(v, n).Normalized();

        Assert.True(r.Y > 0);
        Assert.InRange(r.X - v.X, -1e-6f, 1e-6f);
    }

    [Fact]
    public void Refract_NormalIncidence_GoesStraight()
    {
        var n = Vec3.UnitY;
        var v = -Vec3.UnitY; // into surface
        bool ok = Optics.Refract(v, n, eta: 1f / 1.5f, out var t);
        Assert.True(ok);

        var tn = t.Normalized();
        Assert.InRange(tn.X, -1e-6f, 1e-6f);
        Assert.InRange(tn.Z, -1e-6f, 1e-6f);
        Assert.True(tn.Y < -0.999f);
    }

    [Fact]
    public void Schlick_NormalIncidence_MatchesR0()
    {
        float r = Optics.Schlick(1f, 1f, 1.5f);
        Assert.InRange(r, 0.039f, 0.041f);
    }
}
