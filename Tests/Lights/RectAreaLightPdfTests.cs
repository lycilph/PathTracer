using Core.Lights;
using Core.Math;

namespace Tests.Lights;

public class RectAreaLightPdfTests
{
    [Fact]
    public void Pdf_IsFiniteAndPositive_WhenLookingAtLight()
    {
        var light = new RectAreaLightXZ(0, 2, 0, 2, k: 1, normal: -Vec3.UnitY, radiance: new Vec3(1, 1, 1));
        var refPoint = new Vec3(1, 0, 1);
        var wi = (new Vec3(1, 1, 1) - refPoint).Normalized();

        float pdf = light.Pdf(refPoint, wi);
        Assert.True(pdf > 0f);
        Assert.False(float.IsNaN(pdf) || float.IsInfinity(pdf));
    }

    [Fact]
    public void Pdf_IsZero_WhenRayMissesLight()
    {
        var light = new RectAreaLightXZ(0, 1, 0, 1, k: 1, normal: -Vec3.UnitY, radiance: new Vec3(1, 1, 1));
        var refPoint = new Vec3(10, 0, 10);
        var wi = Vec3.UnitY;
        float pdf = light.Pdf(refPoint, wi);
        Assert.Equal(0f, pdf);
    }
}
