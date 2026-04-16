using Core.Materials;
using Core.Math;
using Core.Random;
using Core.Sampling;
using Core.Scene;

namespace Tests.Materials;

public class LambertianSamplingTests
{
    [Fact]
    public void Sample_ProducesDirectionInUpperHemisphere_AndValidPdf()
    {
        var lambert = new Lambertian(new Vec3(0.8f, 0.7f, 0.6f));
        var sampler = new Sampler(new Pcg32(42));

        var rayIn = new Ray(Vec3.Zero, Vec3.UnitZ);
        var hit = new HitRecord(
            point: Vec3.Zero,
            outwardNormal: Vec3.UnitY,
            t: 1f,
            ray: rayIn,
            material: lambert);

        var wo = (-rayIn.Direction).Normalized();

        for (int i = 0; i < 10_000; i++)
        {
            Assert.True(lambert.Sample(wo, hit, sampler, out var wi, out var pdf, out var f));
            Assert.True(pdf > 0f);

            float cos = Vec3.Dot(wi.Normalized(), hit.Normal);
            Assert.True(cos >= 0f);

            // f should be albedo/pi for Lambertian when wi is in hemisphere
            Assert.False(f.NearZero());
        }
    }


    [Fact]
    public void Pdf_MatchesCosineOverPi()
    {
        var lambert = new Lambertian(new Vec3(1f, 1f, 1f));

        // Make sure the HitRecord ends up with Normal = +Y.
        // If ray direction has negative dot with outward normal, FrontFace=true and Normal=outward.
        var rayIn = new Ray(Vec3.Zero, -Vec3.UnitY);
        var hit = new HitRecord(
            point: Vec3.Zero,
            outwardNormal: Vec3.UnitY,
            t: 1f,
            ray: rayIn,
            material: lambert);

        Assert.True(hit.FrontFace);
        Assert.Equal(Vec3.UnitY, hit.Normal);

        var wo = (-rayIn.Direction).Normalized(); // wo = +Y (not important for Lambertian pdf)
        var wi = Vec3.UnitY;                      // cos(theta)=1

        float pdf = lambert.Pdf(wo, wi, hit);

        // Expected: cos/pi = 1/pi
        float expected = MathUtil.InvPi;
        Assert.InRange(pdf, expected - 1e-6f, expected + 1e-6f);
    }

}