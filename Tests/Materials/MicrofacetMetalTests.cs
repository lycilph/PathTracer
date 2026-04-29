using Core.Materials;
using Core.Math;
using Core.Random;
using Core.Sampling;
using Core.Scene;

namespace Tests.Materials;

public class MicrofacetMetalTests
{

    [Fact]
    public void Sample_ReturnsMostlyValidDirections_AndConsistentPdf()
    {
        var mat = new MicrofacetMetal(new Vec3(0.9f, 0.7f, 0.6f), roughness: 0.3f);
        var sampler = new Sampler(new Pcg32(123));

        var rayIn = new Ray(Vec3.Zero, -Vec3.UnitY);
        var hit = new HitRecord(Vec3.Zero, Vec3.UnitY, 1f, rayIn, mat);

        var wo = (-rayIn.Direction).Normalized(); // +Y

        int attempts = 5000;
        int accepted = 0;

        for (int i = 0; i < attempts; i++)
        {
            if (!mat.Sample(wo, hit, sampler, out var wi, out var pdf, out var f))
                continue;

            accepted++;

            // Must be above the surface
            Assert.True(Vec3.Dot(wi, hit.Normal) > 0f);

            // Pdf consistency
            float pdf2 = mat.Pdf(wo, wi, hit);
            Assert.InRange(pdf - pdf2, -1e-5f, 1e-5f);

            // f finite
            Assert.False(float.IsNaN(f.X) || float.IsNaN(f.Y) || float.IsNaN(f.Z));
            Assert.False(float.IsInfinity(f.X) || float.IsInfinity(f.Y) || float.IsInfinity(f.Z));
        }

        // With rejection sampling in Sample(), this should be extremely high.
        Assert.True(accepted > attempts * 0.95f, $"Acceptance too low: {accepted}/{attempts}");
    }


    [Fact]
    public void Evaluate_IsZeroBelowSurface()
    {
        var mat = new MicrofacetMetal(new Vec3(0.9f, 0.9f, 0.9f), roughness: 0.2f);
        var rayIn = new Ray(Vec3.Zero, -Vec3.UnitY);
        var hit = new HitRecord(Vec3.Zero, Vec3.UnitY, 1f, rayIn, mat);
        var wo = Vec3.UnitY;

        // wi below surface
        var wi = -Vec3.UnitY;
        var f = mat.Evaluate(wo, wi, hit);
        Assert.True(f.NearZero());
    }
}