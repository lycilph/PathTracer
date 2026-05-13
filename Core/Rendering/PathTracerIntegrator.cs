
using Core.Camera;
using Core.Math;
using Core.Random;
using Core.Rendering.Debugging;
using Core.Sampling;
using Core.Scene;

namespace Core.Rendering;

public sealed class PathTracerIntegrator : IProgressiveIntegrator
{
    private int _width;
    private int _height;
    private Scene.Scene? _scene;
    private ICamera? _camera;

    public void Initialize(
        int width,
        int height,
        Scene.Scene scene,
        ICamera camera)
    {
        _width = width;
        _height = height;
        _scene = scene;
        _camera = camera;
    }

    public void RenderIteration(
        AccumulationBuffer accumulation,
        int iteration,
        CancellationToken token)
    {
        if (_scene is null || _camera is null)
            throw new InvalidOperationException();

        Parallel.For(0, _height, y =>
        {
            for (int x = 0; x < _width; x++)
            {
                ulong seed = SeedHash.PixelSampleSeed(
                    x,
                    y,
                    iteration,
                    1234);

                var rng = new Pcg32(seed);
                var sampler = new Sampler(rng);

                float u = (x + sampler.Next1D()) / _width;
                float v = (y + sampler.Next1D()) / _height;

                var ray = _camera.GetRay(u, 1f - v, sampler);

                Vec3 c = PathTracer.EvaluateRay(ray, _scene, sampler);

                accumulation.AddSample(x, y, c);
            }
        });
    }

    public IReadOnlyList<DebugFrame> GetDebugFrames()
        => [];

    public RenderStatistics GetStatistics()
        => new();
}
