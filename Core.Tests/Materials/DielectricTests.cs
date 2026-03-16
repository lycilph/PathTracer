using Core.Materials;
using FluentAssertions;

namespace Core.Tests.Materials;

public class DielectricTests
{
    // ── Snell's law ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData(30.0)]
    [InlineData(45.0)]
    [InlineData(60.0)]
    public void Refract_SnellsLaw_AngleSatisfiesRelation(double incidentDegrees)
    {
        // η_i * sinθ_i = η_t * sinθ_t  →  sinθ_t = sinθ_i / ior
        const double ior = 1.5;
        const double etaRatio = 1.0 / ior; // air → glass
        var incidentRad = incidentDegrees * Math.PI / 180.0;

        // Incident ray in XY plane hitting a Y-normal surface
        var d = new Vector3(Math.Sin(incidentRad), -Math.Cos(incidentRad), 0);
        var n = Vector3.UnitY;

        var refracted = Dielectric.Refract(d, n, etaRatio);

        // Measure the refracted angle from the normal
        var cosRefracted = Vector3.Dot(refracted, -n);
        var sinRefracted = Math.Sqrt(1.0 - cosRefracted * cosRefracted);
        var sinExpected = Math.Sin(incidentRad) * etaRatio;

        sinRefracted.Should().BeApproximately(sinExpected, 1e-10,
            because: $"Snell's law must hold for θ_i = {incidentDegrees}°");
    }

    [Fact]
    public void Refract_NormalIncidence_RayPassesStraightThrough()
    {
        // Ray hitting surface straight on should not bend
        var d = -Vector3.UnitY;
        var n = Vector3.UnitY;
        var refracted = Dielectric.Refract(d, n, 1.0 / 1.5);

        refracted.X.Should().BeApproximately(0.0, 1e-10);
        refracted.Z.Should().BeApproximately(0.0, 1e-10);
        refracted.Y.Should().BeLessThan(0); // continues in same general direction
    }

    // ── Total Internal Reflection ─────────────────────────────────────────────

    [Fact]
    public void Scatter_TotalInternalReflection_AlwaysReflects()
    {
        // Critical angle for glass (ior=1.5): arcsin(1/1.5) ≈ 41.8°
        // Use 50° — well past the critical angle
        const double ior = 1.5;
        const double angle = 50.0 * Math.PI / 180.0;

        // Ray inside glass hitting surface at 50° — must TIR
        var direction = new Vector3(Math.Sin(angle), Math.Cos(angle), 0);
        var ray = new Ray(new Vector3(-0.5, -1, 0), direction);

        // FrontFace = false means ray is inside the glass
        var hit = new HitRecord
        {
            T = 1.0,
            Point = Vector3.Zero,
            Normal = -Vector3.UnitY, // flipped to oppose ray (inside surface)
            FrontFace = false
        };

        var mat = new Dielectric(ior);
        var sampler = new Sampler(seed: 42);

        // Run many times — every single one must reflect (TIR is deterministic)
        for (var i = 0; i < 100; i++)
        {
            mat.Scatter(ray, hit, sampler, out _, out var scattered);
            // Reflected ray must stay on the same side as the normal
            Vector3.Dot(scattered.Direction, hit.Normal)
                .Should().BeGreaterThan(0,
                    because: "TIR must always reflect");
        }
    }

    // ── Fresnel ───────────────────────────────────────────────────────────────

    [Fact]
    public void Schlick_NormalIncidence_ReturnsF0()
    {
        // At θ=0°, F = F0 = ((1-eta)/(1+eta))²
        const double etaRatio = 1.0 / 1.5;
        var f0 = Math.Pow((1.0 - etaRatio) / (1.0 + etaRatio), 2);
        var result = Dielectric.Schlick(1.0, etaRatio); // cosθ = 1 → θ = 0°

        result.Should().BeApproximately(f0, 1e-10);
    }

    [Fact]
    public void Schlick_GrazingAngle_ReturnsOne()
    {
        // At θ=90° (grazing), all light reflects — F = 1
        var result = Dielectric.Schlick(0.0, 1.0 / 1.5); // cosθ = 0 → θ = 90°
        result.Should().BeApproximately(1.0, 1e-10);
    }

    // ── General ───────────────────────────────────────────────────────────────

    [Fact]
    public void Scatter_AttenuationIsAlwaysOne()
    {
        // Glass absorbs no light — attenuation must always be (1,1,1)
        var mat = new Dielectric(1.5);
        var sampler = new Sampler(seed: 42);
        var ray = new Ray(new Vector3(0, 0, -5), Vector3.UnitZ);
        var hit = HitRecord.Create(1.0, Vector3.Zero, ray, -Vector3.UnitZ, mat);

        for (var i = 0; i < 100; i++)
        {
            mat.Scatter(ray, hit, sampler, out var attenuation, out _);
            attenuation.Should().Be(Vector3.One);
        }
    }

    [Fact]
    public void Scatter_AlwaysReturnsTrue()
    {
        // Dielectric always scatters (either reflects or refracts)
        var mat = new Dielectric(1.5);
        var sampler = new Sampler(seed: 42);
        var ray = new Ray(new Vector3(0, 0, -5), Vector3.UnitZ);
        var hit = HitRecord.Create(1.0, Vector3.Zero, ray, -Vector3.UnitZ, mat);

        for (var i = 0; i < 100; i++)
            mat.Scatter(ray, hit, sampler, out _, out _).Should().BeTrue();
    }
}