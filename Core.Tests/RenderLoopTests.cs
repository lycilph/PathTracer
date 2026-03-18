using Core.Materials;
using FluentAssertions;

namespace Core.Tests;

public class RenderLoopTests
{
    private static (SceneList scene, Camera camera, FrameBuffer fb,
                    PathIntegrator integrator, RenderLoop loop) Setup(
        int width = 32, int height = 32)
    {
        var scene = new SceneList();

        // A bright emissive sphere filling most of the view
        scene.Add(new Sphere(
            new Vector3(0, 0, 3), 2.0,
            new Emissive(new Vector3(1, 1, 1))));

        var camera = new Camera(
            position: new Vector3(0, 0, 0),
            lookAt: new Vector3(0, 0, 1),
            up: Vector3.UnitY,
            vFovDegrees: 90,
            imageWidth: width,
            imageHeight: height);

        var fb = new FrameBuffer(width, height);
        var integrator = new PathIntegrator { BackgroundRadiance = Vector3.Zero };
        var loop = new RenderLoop();

        return (scene, camera, fb, integrator, loop);
    }

    [Fact]
    public void Render_FillsFrameBuffer_WithSamples()
    {
        var (scene, camera, fb, integrator, loop) = Setup();

        loop.Render(scene, camera, fb, integrator, samplesPerPixel: 4, TestContext.Current.CancellationToken);

        // Every pixel should have exactly 4 samples
        for (var y = 0; y < fb.Height; y++)
            for (var x = 0; x < fb.Width; x++)
                fb.GetSampleCount(x, y).Should().Be(4);
    }

    [Fact]
    public void Render_EmissiveScene_ProducesNonBlackPixels()
    {
        var (scene, camera, fb, integrator, loop) = Setup();

        loop.Render(scene, camera, fb, integrator, samplesPerPixel: 4, TestContext.Current.CancellationToken);

        // At least some pixels should be non-black given the large emissive sphere
        var nonBlack = 0;
        for (var y = 0; y < fb.Height; y++)
            for (var x = 0; x < fb.Width; x++)
            {
                var (r, g, b) = fb.GetDisplayPixel(x, y);
                if (r > 0 || g > 0 || b > 0) nonBlack++;
            }

        nonBlack.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Render_CancellationRequested_StopsEarly()
    {
        // Use a large image and high spp to guarantee work is still in progress
        var (scene, camera, fb, integrator, loop) = Setup(width: 256, height: 256);
        using var cts = new CancellationTokenSource();

        var tileCount = 0;

        // Cancel after the very first tile completes
        void OnTileComplete()
        {
            if (Interlocked.Increment(ref tileCount) == 5)
                cts.Cancel();
        }

        try
        {
            loop.Render(scene, camera, fb, integrator,
                samplesPerPixel: 1000,
                cancellationToken: cts.Token,
                onTileComplete: OnTileComplete);
        }
        catch (OperationCanceledException) { /* expected */ }

        // Only a fraction of tiles should have completed
        var totalTiles = (int)Math.Ceiling(256.0 / loop.TileSize) *
                         (int)Math.Ceiling(256.0 / loop.TileSize);

        tileCount.Should().BeLessThan(totalTiles,
            because: "rendering should have stopped before all tiles completed");
    }

    [Fact]
    public void Render_TileCallback_IsInvokedForEachTile()
    {
        var (scene, camera, fb, integrator, loop) = Setup(width: 32, height: 32);

        // 32×32 image with 16×16 tiles = 4 tiles
        var callbackCount = 0;
        loop.Render(scene, camera, fb, integrator,
            samplesPerPixel: 1,
            TestContext.Current.CancellationToken,
            onTileComplete: () => Interlocked.Increment(ref callbackCount));

        callbackCount.Should().Be(4);
    }

    [Fact]
    public void Render_MoreSamplesPerPixel_ReducesVariance()
    {
        // With more samples the per-pixel values should converge and
        // have less spread (lower standard deviation)
        double ComputeStdDev(int spp)
        {
            var (scene, camera, fb, integrator, loop) = Setup();
            loop.Render(scene, camera, fb, integrator, samplesPerPixel: spp);

            var values = new List<double>();
            for (var y = 0; y < fb.Height; y++)
                for (var x = 0; x < fb.Width; x++)
                    values.Add(fb.GetPixelRadiance(x, y).X);

            var mean = values.Average();
            var variance = values.Select(v => (v - mean) * (v - mean)).Average();
            return Math.Sqrt(variance);
        }

        var stdDev1 = ComputeStdDev(1);
        var stdDev16 = ComputeStdDev(16);

        stdDev16.Should().BeLessThan(stdDev1,
            because: "more samples per pixel must reduce variance");
    }
}