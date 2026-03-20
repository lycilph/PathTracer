using Core.Algebra;
using FluentAssertions;


namespace ScriptApi.Tests;

public class SceneBuilderTests
{
    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public void Build_ValidScene_ReturnsNoErrors()
    {
        var result = BuildMinimalScene();

        result.Validation.IsValid.Should().BeTrue();
        result.Validation.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Build_ValidScene_CameraIsConfigured()
    {
        var result = BuildMinimalScene();

        result.Camera.Should().NotBeNull();
        result.Camera.ImageWidth.Should().Be(512);
        result.Camera.ImageHeight.Should().Be(512);
    }

    [Fact]
    public void Build_ValidScene_SettingsAreConfigured()
    {
        var result = BuildMinimalScene();

        result.Settings.ImageWidth.Should().Be(512);
        result.Settings.ImageHeight.Should().Be(512);
        result.Settings.SamplesPerPixel.Should().Be(64);
        result.Settings.BackgroundRadiance.Should().Be(Vector3.Zero);
    }

    [Fact]
    public void Build_ValidScene_SceneIsNotNull()
    {
        var result = BuildMinimalScene();

        result.Scene.Should().NotBeNull();
    }

    [Fact]
    public void Build_WithAreaLight_LightsListIsPopulated()
    {
        var result = BuildMinimalScene();

        result.Lights.Should().HaveCount(1);
    }

    // ── Validation errors ─────────────────────────────────────────────────────

    [Fact]
    public void Build_NoCamera_ReturnsError()
    {
        var result = Scene
            .WithCamera(Vector3.Zero, -Vector3.UnitZ, 40)
            .WithRenderSettings(512, 512, 64)
            .Build();

        // Remove camera by building without it
        var noCamera = new SceneBuilder()
            .WithRenderSettings(512, 512, 64)
            .Build();

        noCamera.Validation.IsValid.Should().BeFalse();
        noCamera.Validation.Errors.Should().ContainSingle(
            e => e.Message.Contains("No camera"));
    }

    [Fact]
    public void Build_NoRenderSettings_ReturnsError()
    {
        var result = new SceneBuilder()
            .WithCamera(Vector3.Zero, -Vector3.UnitZ, 40)
            .Build();

        result.Validation.IsValid.Should().BeFalse();
        result.Validation.Errors.Should().ContainSingle(
            e => e.Message.Contains("No render settings"));
    }

    [Fact]
    public void Build_ZeroRadius_ReturnsError()
    {
        var result = Scene
            .WithCamera(new Vector3(0, 0, 3.5), Vector3.Zero, 40)
            .WithRenderSettings(512, 512, 64)
            .AddSphere(
                centre: Vector3.Zero,
                radius: 0,
                material: MaterialBuilder.Lambertian(Vector3.One),
                name: "BadSphere")
            .Build();

        result.Validation.IsValid.Should().BeFalse();
        result.Validation.Errors.Should().ContainSingle(
            e => e.PrimitiveName == "BadSphere");
    }

    [Fact]
    public void Build_SamePositionAndLookAt_ReturnsError()
    {
        var result = Scene
            .WithCamera(Vector3.Zero, Vector3.Zero, 40)
            .WithRenderSettings(512, 512, 64)
            .Build();

        result.Validation.IsValid.Should().BeFalse();
        result.Validation.Errors.Should().ContainSingle(
            e => e.Message.Contains("look-at"));
    }

    [Fact]
    public void Build_NegativeImageSize_ReturnsError()
    {
        var result = new SceneBuilder()
            .WithCamera(new Vector3(0, 0, 3.5), Vector3.Zero, 40)
            .WithRenderSettings(-1, 512, 64)
            .Build();

        result.Validation.IsValid.Should().BeFalse();
        result.Validation.Errors.Should().ContainSingle(
            e => e.Message.Contains("Image width"));
    }

    // ── Validation warnings ───────────────────────────────────────────────────

    [Fact]
    public void Build_NoLights_ReturnsWarning()
    {
        var result = Scene
            .WithCamera(new Vector3(0, 0, 3.5), Vector3.Zero, 40)
            .WithRenderSettings(512, 512, 64)
            .AddSphere(
                centre: Vector3.Zero,
                radius: 0.5,
                material: MaterialBuilder.Lambertian(Vector3.One))
            .Build();

        result.Validation.HasWarnings.Should().BeTrue();
        result.Validation.Warnings.Should().ContainSingle(
            w => w.Message.Contains("No lights"));
    }

    [Fact]
    public void Build_LowSamplesPerPixel_ReturnsWarning()
    {
        var result = Scene
            .WithCamera(new Vector3(0, 0, 3.5), Vector3.Zero, 40)
            .WithRenderSettings(512, 512, 2)
            .Build();

        result.Validation.HasWarnings.Should().BeTrue();
        result.Validation.Warnings.Should().ContainSingle(
            w => w.Message.Contains("very low"));
    }

    [Fact]
    public void Build_AlbedoAboveOne_ReturnsWarning()
    {
        var result = Scene
            .WithCamera(new Vector3(0, 0, 3.5), Vector3.Zero, 40)
            .WithRenderSettings(512, 512, 64)
            .AddSphere(
                centre: Vector3.Zero,
                radius: 0.5,
                material: MaterialBuilder.Lambertian(new Vector3(1.5, 1.5, 1.5)),
                name: "BrightSphere")
            .Build();

        result.Validation.Warnings.Should().ContainSingle(
            w => w.PrimitiveName == "BrightSphere" &&
                 w.Message.Contains("energy conservation"));
    }

    [Fact]
    public void Build_DuplicateNames_ReturnsWarning()
    {
        var result = Scene
            .WithCamera(new Vector3(0, 0, 3.5), Vector3.Zero, 40)
            .WithRenderSettings(512, 512, 64)
            .AddSphere(Vector3.Zero, 0.5,
                MaterialBuilder.Lambertian(Vector3.One), name: "Ball")
            .AddSphere(new Vector3(2, 0, 0), 0.5,
                MaterialBuilder.Lambertian(Vector3.One), name: "Ball")
            .Build();

        result.Validation.Warnings.Should().ContainSingle(
            w => w.Message.Contains("Duplicate"));
    }

    // ── Cornell Box smoke test ────────────────────────────────────────────────

    [Fact]
    public void Build_CornellBox_IsValidWithNoWarnings()
    {
        var result = BuildCornellBox();

        result.Validation.IsValid.Should().BeTrue();
        result.Validation.HasWarnings.Should().BeFalse();
        result.Lights.Should().HaveCount(1);
        result.Scene.Should().NotBeNull();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static SceneDescription BuildMinimalScene() =>
        Scene
            .WithCamera(
                position: new Vector3(0, 0, 3.5),
                lookAt: Vector3.Zero,
                fovDegrees: 40)
            .WithRenderSettings(512, 512, 64)
            .AddSphere(
                centre: Vector3.Zero,
                radius: 0.5,
                material: MaterialBuilder.Lambertian(new Vector3(0.8, 0.5, 0.3)),
                name: "TestSphere")
            .AddAreaLight(
                corner: new Vector3(-0.25, 0.999, -0.25),
                edge1: new Vector3(0.5, 0, 0),
                edge2: new Vector3(0, 0, 0.5),
                emission: new Vector3(15, 15, 15),
                name: "CeilingLight")
            .Build();

    private static SceneDescription BuildCornellBox() =>
        Scene
            .WithCamera(
                position: new Vector3(0, 0, 3.5),
                lookAt: Vector3.Zero,
                fovDegrees: 40)
            .WithRenderSettings(512, 512, 256)
            .AddQuad(new Vector3(-1, -1, -1), new Vector3(2, 0, 0),
                new Vector3(0, 0, 2),
                MaterialBuilder.Lambertian(new Vector3(0.73, 0.73, 0.73)),
                name: "Floor")
            .AddQuad(new Vector3(-1, 1, -1), new Vector3(2, 0, 0),
                new Vector3(0, 0, 2),
                MaterialBuilder.Lambertian(new Vector3(0.73, 0.73, 0.73)),
                name: "Ceiling")
            .AddQuad(new Vector3(-1, -1, -1), new Vector3(2, 0, 0),
                new Vector3(0, 2, 0),
                MaterialBuilder.Lambertian(new Vector3(0.73, 0.73, 0.73)),
                name: "BackWall")
            .AddQuad(new Vector3(-1, -1, -1), new Vector3(0, 2, 0),
                new Vector3(0, 0, 2),
                MaterialBuilder.Lambertian(new Vector3(0.65, 0.05, 0.05)),
                name: "LeftWall")
            .AddQuad(new Vector3(1, -1, -1), new Vector3(0, 2, 0),
                new Vector3(0, 0, 2),
                MaterialBuilder.Lambertian(new Vector3(0.12, 0.45, 0.15)),
                name: "RightWall")
            .AddSphere(
                centre: new Vector3(0.35, -0.55, 0.2),
                radius: 0.45,
                material: MaterialBuilder.Dielectric(ior: 1.5),
                name: "GlassBall")
            .AddSphere(
                centre: new Vector3(-0.35, -0.55, -0.2),
                radius: 0.45,
                material: MaterialBuilder.GgxMetal(
                    new Vector3(0.95, 0.93, 0.88), roughness: 0.05),
                name: "SilverBall")
            .AddAreaLight(
                corner: new Vector3(-0.25, 0.999, -0.25),
                edge1: new Vector3(0.5, 0, 0),
                edge2: new Vector3(0, 0, 0.5),
                emission: new Vector3(15, 15, 15),
                name: "CeilingLight")
            .Build();
}