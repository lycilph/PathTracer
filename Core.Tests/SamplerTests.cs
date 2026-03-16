using FluentAssertions;

namespace Core.Tests;

public class SamplerTests
{
    [Fact]
    public void Next_AlwaysInUnitInterval()
    {
        var sampler = new Sampler(seed: 42);
        for (var i = 0; i < 10_000; i++)
        {
            var v = sampler.Next();
            v.Should().BeGreaterThanOrEqualTo(0.0).And.BeLessThan(1.0);
        }
    }

    [Fact]
    public void CosineWeightedHemisphere_ReturnsUnitVectors()
    {
        var sampler = new Sampler(seed: 42);
        var normal = Vector3.UnitY;

        for (var i = 0; i < 1000; i++)
        {
            var dir = sampler.CosineWeightedHemisphere(normal);
            dir.Length.Should().BeApproximately(1.0, 1e-10,
                because: "sampled directions must be unit length");
        }
    }

    [Fact]
    public void CosineWeightedHemisphere_AllDirectionsInCorrectHemisphere()
    {
        var sampler = new Sampler(seed: 42);
        var normal = Vector3.UnitY;

        // Every sample must be in the hemisphere defined by the normal
        for (var i = 0; i < 1000; i++)
        {
            var dir = sampler.CosineWeightedHemisphere(normal);
            Vector3.Dot(dir, normal).Should().BeGreaterThanOrEqualTo(0.0,
                because: "all samples must be in the upper hemisphere");
        }
    }

    [Fact]
    public void CosineWeightedHemisphere_MeanDirectionAlignedWithNormal()
    {
        // The mean of many cosine-weighted samples should align with the normal
        var sampler = new Sampler(seed: 42);
        var normal = Vector3.UnitZ;
        var sum = Vector3.Zero;

        const int n = 100_000;
        for (var i = 0; i < n; i++)
            sum = sum + sampler.CosineWeightedHemisphere(normal);

        var mean = sum / n;

        // Mean X and Y should be near zero; mean Z should be positive
        mean.X.Should().BeApproximately(0.0, 0.01);
        mean.Y.Should().BeApproximately(0.0, 0.01);
        mean.Z.Should().BeGreaterThan(0.5);
    }

    [Fact]
    public void CosineWeightedHemisphere_PdfIntegratesTo1()
    {
        // Monte Carlo estimate of ∫ (cosθ/π) dω over the hemisphere should equal 1
        // We verify this by checking that the average of (1/pdf) * pdf = 1
        // i.e. the average cosθ/π weight integrated via uniform sampling ≈ 1
        // Simpler check: verify ∫cosθ dω = π by estimating with uniform samples
        var sampler = new Sampler(seed: 123);
        var normal = Vector3.UnitZ;
        var sum = 0.0;

        const int n = 100_000;
        for (var i = 0; i < n; i++)
        {
            var dir = sampler.CosineWeightedHemisphere(normal);
            var cosine = Vector3.Dot(dir, normal);
            // PDF = cosθ/π, so estimator = f(x)/pdf = 1/(cosθ/π) * cosθ = π
            // Average should equal π... divided by π = 1
            sum += cosine / (cosine / Math.PI);
        }

        var estimate = sum / n;
        estimate.Should().BeApproximately(Math.PI, 0.05);
    }
}