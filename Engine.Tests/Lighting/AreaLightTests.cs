using Core.Algebra;
using Core.Sampling;
using Engine.Lighting;
using FluentAssertions;

namespace Engine.Tests.Lighting;

public class AreaLightTests
{
    // A 1×1 light at y=2, facing downward
    private static AreaLight MakeLight() => new(
        corner: new Vector3(-0.5, 2, -0.5),
        edge1: new Vector3(1, 0, 0),
        edge2: new Vector3(0, 0, 1),
        emission: new Vector3(10, 10, 10));

    [Fact]
    public void Sample_ReturnedPointIsOnLightSurface()
    {
        var light = MakeLight();
        var sampler = new Sampler(seed: 42);
        var origin = Vector3.Zero;

        for (var i = 0; i < 100; i++)
        {
            light.Sample(origin, sampler,
                out var point, out _, out _);

            // Point must be within the light's XZ bounds
            point.X.Should().BeInRange(-0.5, 0.5);
            point.Z.Should().BeInRange(-0.5, 0.5);
            point.Y.Should().BeApproximately(2.0, 1e-10);
        }
    }

    [Fact]
    public void Sample_EmissionMatchesConstructor()
    {
        var emission = new Vector3(10, 10, 10);
        var light = MakeLight();
        var sampler = new Sampler(seed: 42);

        light.Sample(Vector3.Zero, sampler,
            out _, out _, out var sampledEmission);

        sampledEmission.Should().Be(emission);
    }

    [Fact]
    public void Sample_PdfIsPositive()
    {
        var light = MakeLight();
        var sampler = new Sampler(seed: 42);

        var pdf = light.Sample(Vector3.Zero, sampler,
            out _, out _, out _);

        pdf.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Sample_CloserOrigin_ProducesLowerPdf()
    {
        // Closer origins subtend a larger solid angle.
        // PDF_ω = dist² / (cosθ · A) — smaller distance → smaller PDF.
        var light = MakeLight();
        var sampler1 = new Sampler(seed: 42);
        var sampler2 = new Sampler(seed: 42); // same seed = same point on light

        var pdfClose = light.Sample(new Vector3(0, 1.9, 0), sampler1,
            out _, out _, out _);
        var pdfFar = light.Sample(new Vector3(0, -10, 0), sampler2,
            out _, out _, out _);

        pdfClose.Should().BeLessThan(pdfFar);
    }

    [Fact]
    public void Hit_RayThroughLight_ReturnsHit()
    {
        var light = MakeLight();
        var ray = new Ray(Vector3.Zero, Vector3.UnitY);

        light.Hit(ray, out var hit).Should().BeTrue();
        hit.T.Should().BeApproximately(2.0, 0.01);
    }

    [Fact]
    public void GetBounds_ContainsLightSurface()
    {
        var light = MakeLight();
        var bounds = light.GetBounds();

        bounds.Min.X.Should().BeLessThanOrEqualTo(-0.5);
        bounds.Min.Z.Should().BeLessThanOrEqualTo(-0.5);
        bounds.Max.X.Should().BeGreaterThanOrEqualTo(0.5);
        bounds.Max.Z.Should().BeGreaterThanOrEqualTo(0.5);
        bounds.Max.Y.Should().BeGreaterThanOrEqualTo(2.0);
    }
}