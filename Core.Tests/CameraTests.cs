using FluentAssertions;

namespace Core.Tests;

public class CameraTests
{
    // Standard camera setup: sitting at Z=3, looking at origin, 90° fov, 100×100
    private static Camera MakeCamera(double fov = 90.0, int w = 100, int h = 100) =>
        new(position: new Vector3(0, 0, 3),
            lookAt: Vector3.Zero,
            up: Vector3.UnitY,
            vFovDegrees: fov,
            imageWidth: w,
            imageHeight: h);

    [Fact]
    public void GenerateRay_OriginIsCorrect()
    {
        var cam = MakeCamera();
        var ray = cam.GenerateRay(50, 50);
        ray.Origin.Should().Be(new Vector3(0, 0, 3));
    }

    [Fact]
    public void GenerateRay_CentrePixel_AimsAtLookAt()
    {
        // Centre pixel with 0.5 jitter should aim roughly along -Z toward origin
        var cam = MakeCamera();
        var ray = cam.GenerateRay(50, 50, 0.5, 0.5);

        // Direction should point toward -Z (away from camera toward origin)
        ray.Direction.Z.Should().BeLessThan(0);

        // X and Y should be near zero for the centre pixel
        ray.Direction.X.Should().BeApproximately(0.0, 0.01);
        ray.Direction.Y.Should().BeApproximately(0.0, 0.01);
    }

    [Fact]
    public void GenerateRay_DirectionIsUnitLength()
    {
        var cam = MakeCamera();

        // Check several pixels across the image
        foreach (var (i, j) in new[] { (0, 0), (99, 99), (0, 99), (99, 0), (50, 50) })
        {
            var ray = cam.GenerateRay(i, j);
            ray.Direction.Length.Should().BeApproximately(1.0, 1e-10,
                because: $"pixel ({i},{j}) direction must be unit length");
        }
    }

    [Fact]
    public void GenerateRay_TopLeftPixel_HasNegativeXPositiveY()
    {
        // Top-left pixel should aim up and to the left
        var cam = MakeCamera();
        var ray = cam.GenerateRay(0, 0, 0.5, 0.5);
        ray.Direction.X.Should().BeLessThan(0);
        ray.Direction.Y.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GenerateRay_BottomRightPixel_HasPositiveXNegativeY()
    {
        // Bottom-right pixel should aim down and to the right
        var cam = MakeCamera();
        var ray = cam.GenerateRay(99, 99, 0.5, 0.5);
        ray.Direction.X.Should().BeGreaterThan(0);
        ray.Direction.Y.Should().BeLessThan(0);
    }

    [Fact]
    public void GenerateRay_WiderFov_ProducesWiderSpread()
    {
        // A wider FOV should produce a larger X component for the corner pixel
        var narrowCam = MakeCamera(fov: 30);
        var wideCam = MakeCamera(fov: 90);

        var narrowRay = narrowCam.GenerateRay(99, 50, 0.5, 0.5);
        var wideRay = wideCam.GenerateRay(99, 50, 0.5, 0.5);

        wideRay.Direction.X.Should().BeGreaterThan(narrowRay.Direction.X);
    }
}