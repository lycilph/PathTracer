using Core.Materials;
using FluentAssertions;

namespace Core.Tests.Materials;

public class EmissiveTests
{
    [Fact]
    public void Emit_ReturnsConfiguredEmission()
    {
        var emission = new Vector3(5.0, 4.0, 3.0);
        var mat = new Emissive(emission);

        mat.Emit().Should().Be(emission);
    }

    [Fact]
    public void Scatter_AlwaysReturnsFalse()
    {
        // Emissive surfaces terminate the path — never scatter
        var mat = new Emissive(new Vector3(10, 10, 10));
        var sampler = new Sampler(seed: 42);
        var ray = new Ray(new Vector3(0, 0, -5), Vector3.UnitZ);
        var hit = HitRecord.Create(5.0, Vector3.Zero, ray, -Vector3.UnitZ,
                          new Emissive(Vector3.One));

        mat.Scatter(ray, hit, sampler, out _, out _).Should().BeFalse();
    }

    [Fact]
    public void Scatter_AttenuationIsZero()
    {
        // No light carried forward from an emissive surface
        var mat = new Emissive(new Vector3(10, 10, 10));
        var sampler = new Sampler(seed: 42);
        var ray = new Ray(new Vector3(0, 0, -5), Vector3.UnitZ);
        var hit = HitRecord.Create(5.0, Vector3.Zero, ray, -Vector3.UnitZ,
                          new Emissive(Vector3.One));

        mat.Scatter(ray, hit, sampler, out var attenuation, out _);

        attenuation.Should().Be(Vector3.Zero);
    }

    [Fact]
    public void Emit_HighDynamicRange_ValuesAboveOne_AreValid()
    {
        // Lights commonly have HDR emission well above 1
        var mat = new Emissive(new Vector3(15.0, 15.0, 15.0));
        mat.Emit().X.Should().Be(15.0);
    }
}