using Core.Acceleration;
using Core.Algebra;
using Core.Geometry;
using Core.Sampling;
using Engine.Integrators;
using Engine.Materials;
using Engine.Rendering;
using FluentAssertions;

namespace Engine.Tests.Rendering;

public class RenderLoopTests
{
    private static (Camera camera, FrameBuffer fb, RenderLoop loop,
                    Func<Ray, Sampler, Vector3> traceFunc) Setup(
        int width = 32, int height = 32)
    {
        var scene = new SceneList();
        scene.Add(new Sphere(
            new Vector3(0, 0, 3), 2.0,
            new Emissive(new Vector3(1, 1, 1))));

        var camera = new Camera(
            new Vector3(0, 0, 0), new Vector3(0, 0, 1),
            Vector3.UnitY, 90, width, height);

        var fb = new FrameBuffer(width, height);
        var integrator = new PathIntegrator { BackgroundRadiance = Vector3.Zero };

        Func<Ray, Sampler, Vector3> traceFunc =
            (ray, sampler) => integrator.Trace(ray, scene, sampler);

        return (camera, fb, new RenderLoop(), traceFunc);
    }

    [Fact]
    public void Render_FillsFrameBuffer_WithSamples()
    {
        var (camera, fb, loop, traceFunc) = Setup();
        loop.Render(camera, fb, traceFunc, samplesPerPixel: 4, TestContext.Current.CancellationToken);

        for (var y = 0; y < fb.Height; y++)
            for (var x = 0; x < fb.Width; x++)
                fb.GetSampleCount(x, y).Should().Be(4);
    }

    [Fact]
    public void Render_EmissiveScene_ProducesNonBlackPixels()
    {
        var (camera, fb, loop, traceFunc) = Setup();
        loop.Render(camera, fb, traceFunc, samplesPerPixel: 4, TestContext.Current.CancellationToken);

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
        var (camera, fb, loop, traceFunc) = Setup(width: 256, height: 256);
        using var cts = new CancellationTokenSource();

        var tileCount = 0;
        void OnTileComplete()
        {
            if (Interlocked.Increment(ref tileCount) == 1)
                cts.Cancel();
        }

        try
        {
            loop.Render(camera, fb, traceFunc,
                samplesPerPixel: 1000,
                cancellationToken: cts.Token,
                onTileComplete: OnTileComplete);
        }
        catch (OperationCanceledException) { /* expected */ }

        var totalTiles = (int)Math.Ceiling(256.0 / loop.TileSize) *
                         (int)Math.Ceiling(256.0 / loop.TileSize);

        tileCount.Should().BeLessThan(totalTiles);
    }

    [Fact]
    public void Render_TileCallback_IsInvokedForEachTile()
    {
        var (camera, fb, loop, traceFunc) = Setup(width: 32, height: 32);

        var callbackCount = 0;
        loop.Render(camera, fb, traceFunc,
            samplesPerPixel: 1,
            TestContext.Current.CancellationToken,
            onTileComplete: () => Interlocked.Increment(ref callbackCount));
        
        callbackCount.Should().Be(4);
    }

    [Fact]
    public void Render_MoreSamplesPerPixel_ReducesVariance()
    {
        double ComputeStdDev(int spp)
        {
            var (camera, fb, loop, traceFunc) = Setup();
            loop.Render(camera, fb, traceFunc, samplesPerPixel: spp);

            var values = new List<double>();
            for (var y = 0; y < fb.Height; y++)
                for (var x = 0; x < fb.Width; x++)
                    values.Add(fb.GetPixelRadiance(x, y).X);

            var mean = values.Average();
            var variance = values.Select(v => (v - mean) * (v - mean)).Average();
            return Math.Sqrt(variance);
        }

        ComputeStdDev(16).Should().BeLessThan(ComputeStdDev(1));
    }
}