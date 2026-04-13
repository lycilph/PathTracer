using Core.Materials;
using Core.Math;
using Core.Random;
using Core.Sampling;

namespace Tests.Materials;

public class LambertianSamplingTests
{
    [Fact]
    public void SampledDirections_AreInUpperHemisphere()
    {
        var lambert = new Lambertian(new Vec3(1));
        var rng = new Pcg32(1);
        var sampler = new Sampler(rng);
        var normal = Vec3.UnitY;

        for (int i = 0; i < 10_000; i++)
        {
            lambert.Sample(normal, sampler, out var wi, out var pdf);

            Assert.True(Vec3.Dot(wi, normal) >= 0f);
            Assert.True(pdf > 0f);
        }
    }

    [Fact]
    public void MeanCosine_IsReasonable()
    {
        var lambert = new Lambertian(new Vec3(1));
        var rng = new Pcg32(42);
        var sampler = new Sampler(rng);
        var normal = Vec3.UnitZ;

        float sumCos = 0f;
        int n = 100_000;

        for (int i = 0; i < n; i++)
        {
            lambert.Sample(normal, sampler, out var wi, out _);
            sumCos += Vec3.Dot(wi.Normalized(), normal);
        }

        float meanCos = sumCos / n;

        // Theoretical expected value for cosine-weighted hemisphere is 2/3 ≈ 0.666
        Assert.InRange(meanCos, 0.63f, 0.70f);
    }
}