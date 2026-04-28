using Core.Materials;
using Core.Math;

namespace Tests.Materials;

public class DielectricTintTests
{
    [Fact]
    public void Transmittance_MatchesExpectedForSimpleCase()
    {
        // If tint=0.5 at distance=1 and strength=1 then sigma=-ln(0.5)
        // T(2) = exp(-sigma*2) = exp(ln(0.5)*2) = 0.25
        var glass = new Dielectric(
            ior: 1.5f,
            tint: new Vec3(0.5f, 0.5f, 0.5f),
            absorptionStrength: 1f);

        var t2 = glass.Transmittance(2f);

        Assert.InRange(t2.X, 0.249f, 0.251f);
        Assert.InRange(t2.Y, 0.249f, 0.251f);
        Assert.InRange(t2.Z, 0.249f, 0.251f);
    }

    [Fact]
    public void ClearGlass_HasUnitTransmittance()
    {
        var glass = new Dielectric(1.5f);
        var t = glass.Transmittance(100f);

        Assert.InRange(t.X, 0.999999f, 1.000001f);
        Assert.InRange(t.Y, 0.999999f, 1.000001f);
        Assert.InRange(t.Z, 0.999999f, 1.000001f);
    }
}