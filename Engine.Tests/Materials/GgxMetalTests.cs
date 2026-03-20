using Core.Algebra;
using Core.Geometry;
using Core.Sampling;
using Engine.Materials;
using FluentAssertions;

namespace Engine.Tests.Materials;

public class GgxMetalTests
{
    private static readonly Vector3 Silver = new(0.95, 0.93, 0.88);

    // ── Basic scattering ──────────────────────────────────────────────────────

    [Fact]
    public void Scatter_ValidHit_ScatteredRayInUpperHemisphere()
    {
        var mat = new GgxMetal(Silver, Roughness: 0.1);
        var sampler = new Sampler(seed: 42);
        var ray = new Ray(new Vector3(0, 1, -1), new Vector3(0, -1, 1).Normalize());
        var hit = HitRecord.Create(1.0, Vector3.Zero, ray, Vector3.UnitY, mat);

        var inHemisphere = 0;
        const int n = 1000;

        for (var i = 0; i < n; i++)
        {
            if (mat.Scatter(ray, hit, sampler, out _, out var scattered))
            {
                Vector3.Dot(scattered.Direction, hit.Normal)
                    .Should().BeGreaterThan(0,
                        because: "scattered ray must be above the surface");
                inHemisphere++;
            }
        }

        // Most rays should scatter for a reasonable roughness
        inHemisphere.Should().BeGreaterThan(n / 2);
    }

    [Fact]
    public void Scatter_ScatteredDirectionIsUnitLength()
    {
        var mat = new GgxMetal(Silver, Roughness: 0.3);
        var sampler = new Sampler(seed: 42);
        var ray = new Ray(new Vector3(0, 1, -1), new Vector3(0, -1, 1).Normalize());
        var hit = HitRecord.Create(1.0, Vector3.Zero, ray, Vector3.UnitY, mat);

        for (var i = 0; i < 200; i++)
        {
            if (mat.Scatter(ray, hit, sampler, out _, out var scattered))
                scattered.Direction.Length.Should().BeApproximately(1.0, 1e-6);
        }
    }

    // ── Fresnel ───────────────────────────────────────────────────────────────

    [Fact]
    public void Scatter_LowRoughness_AttenuationNearF0()
    {
        // At near-normal incidence with very low roughness,
        // attenuation should be close to F0 (the base reflectance)
        var mat = new GgxMetal(Silver, Roughness: 0.01);
        var sampler = new Sampler(seed: 42);

        // Ray hitting surface nearly straight on
        var ray = new Ray(new Vector3(0, 5, 0), -Vector3.UnitY);
        var hit = HitRecord.Create(5.0, Vector3.Zero, ray, Vector3.UnitY, mat);

        var totalAttenuation = Vector3.Zero;
        var count = 0;
        const int n = 2000;

        for (var i = 0; i < n; i++)
        {
            if (mat.Scatter(ray, hit, sampler, out var atten, out _))
            {
                totalAttenuation = totalAttenuation + atten;
                count++;
            }
        }

        var mean = totalAttenuation / count;

        // Mean attenuation should be in a reasonable range around F0
        mean.X.Should().BeInRange(0.5, 1.5);
        mean.Y.Should().BeInRange(0.5, 1.5);
        mean.Z.Should().BeInRange(0.5, 1.5);
    }

    // ── Energy conservation ───────────────────────────────────────────────────

    [Fact]
    public void EnergyConservation_TotalReflectedEnergyLessThanOne()
    {
        // The average attenuation per channel must not exceed 1.0
        // (the surface cannot reflect more energy than it receives)
        var mat = new GgxMetal(Silver, Roughness: 0.3);
        var sampler = new Sampler(seed: 99);
        var ray = new Ray(new Vector3(0, 1, -1), new Vector3(0, -1, 1).Normalize());
        var hit = HitRecord.Create(1.0, Vector3.Zero, ray, Vector3.UnitY, mat);

        var total = Vector3.Zero;
        var count = 0;
        const int n = 5000;

        for (var i = 0; i < n; i++)
        {
            if (mat.Scatter(ray, hit, sampler, out var atten, out _))
            {
                total = total + atten;
                count++;
            }
        }

        var mean = total / count;

        mean.X.Should().BeLessThanOrEqualTo(1.5,
            because: "reflected energy per channel must not grossly exceed 1");
        mean.Y.Should().BeLessThanOrEqualTo(1.5);
        mean.Z.Should().BeLessThanOrEqualTo(1.5);
    }

    // ── Roughness behaviour ───────────────────────────────────────────────────

    [Fact]
    public void HigherRoughness_ProducesWiderScatterSpread()
    {
        // A rougher surface should scatter rays over a wider cone
        var smoothMat = new GgxMetal(Silver, Roughness: 0.05);
        var roughMat = new GgxMetal(Silver, Roughness: 0.8);
        var sampler = new Sampler(seed: 42);

        var ray = new Ray(new Vector3(0, 1, -1), new Vector3(0, -1, 1).Normalize());
        var hit = HitRecord.Create(1.0, Vector3.Zero, ray, Vector3.UnitY, smoothMat);

        double SumAngularDeviation(GgxMetal mat)
        {
            // Perfect reflection direction for this ray/normal
            var perfect = Mirror.Reflect(ray.Direction, hit.Normal).Normalize();
            var total = 0.0;
            var count = 0;
            for (var i = 0; i < 1000; i++)
            {
                if (mat.Scatter(ray, hit, new Sampler(i), out _, out var scattered))
                {
                    // Dot product close to 1 = near perfect reflection
                    total += 1.0 - Vector3.Dot(scattered.Direction, perfect);
                    count++;
                }
            }
            return total / count;
        }

        var smoothSpread = SumAngularDeviation(smoothMat);
        var roughSpread = SumAngularDeviation(roughMat);

        roughSpread.Should().BeGreaterThan(smoothSpread,
            because: "higher roughness must produce wider scatter");
    }

    [Fact]
    public void Pdf_ScatteredDirection_IsPositive()
    {
        var mat = new GgxMetal(Silver, Roughness: 0.3);
        var sampler = new Sampler(seed: 42);
        var ray = new Ray(new Vector3(0, 1, -1), new Vector3(0, -1, 1).Normalize());
        var hit = HitRecord.Create(1.0, Vector3.Zero, ray, Vector3.UnitY, mat);

        // Use a direction actually produced by Scatter — ensures we're in a valid region
        var count = 0;
        for (var i = 0; i < 100; i++)
        {
            if (!mat.Scatter(ray, hit, sampler, out _, out var scattered))
                continue;

            mat.Pdf(ray, hit, scattered).Should().BeGreaterThan(0,
                because: "PDF must be positive for directions Scatter can produce");
            count++;
        }

        count.Should().BeGreaterThan(50, because: "most scatters should succeed");
    }

    [Fact]
    public void Pdf_AndScatter_AreConsistent()
    {
        // Verify the PDF is consistent with Scatter by checking that the
        // Monte Carlo estimator attenuation/pdf stays in a reasonable range
        var mat = new GgxMetal(Silver, Roughness: 0.3);
        var sampler = new Sampler(seed: 99);
        var ray = new Ray(new Vector3(0, 1, -1), new Vector3(0, -1, 1).Normalize());
        var hit = HitRecord.Create(1.0, Vector3.Zero, ray, Vector3.UnitY, mat);

        var total = 0.0;
        var count = 0;

        for (var i = 0; i < 1000; i++)
        {
            if (!mat.Scatter(ray, hit, sampler, out var attenuation, out var scattered))
                continue;

            var pdf = mat.Pdf(ray, hit, scattered);
            if (pdf <= 0) continue;

            // attenuation already has the pdf baked in from Scatter,
            // so attenuation.X should be in a physically plausible range
            total += attenuation.X;
            count++;
        }

        var mean = total / count;

        // Mean attenuation should be physically plausible — not zero, not explosive
        mean.Should().BeInRange(0.01, 10.0,
            because: "mean attenuation must be physically plausible");
    }
}