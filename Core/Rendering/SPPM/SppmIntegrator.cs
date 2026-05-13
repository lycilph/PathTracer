
using Core.Camera;
using Core.Math;
using Core.Rendering.Debugging;
using Core.Scene;

namespace Core.Rendering.SPPM;

public sealed class SppmIntegrator : IProgressiveIntegrator
{
    private readonly SppmConfig _config;

    private int _width;
    private int _height;
    private Scene.Scene? _scene;
    private ICamera? _camera;

    private VisiblePoint[] _visiblePoints = [];
    private SppmPixel[] _pixels = [];

    private readonly List<DebugFrame> _debugFrames = [];
    private readonly RenderStatistics _statistics = new();

    public SppmIntegrator(SppmConfig config)
    {
        _config = config;
    }

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

        _visiblePoints = new VisiblePoint[width * height];
        _pixels = new SppmPixel[width * height];

        for (int i = 0; i < _pixels.Length; i++)
        {
            _pixels[i].Radius = _config.InitialRadius;
        }
    }

    public void RenderIteration(
        AccumulationBuffer accumulation,
        int iteration,
        CancellationToken token)
    {
        if (_scene is null || _camera is null)
            throw new InvalidOperationException();

        _statistics.Iteration = iteration;

        CameraPass.Execute(
            _width,
            _height,
            _scene,
            _camera,
            _visiblePoints,
            _pixels,
            _config,
            token);

        var grid = new PhotonHashGrid(_config.InitialRadius * 2f);

        for (int i = 0; i < _visiblePoints.Length; i++)
        {
            if (_visiblePoints[i].Valid)
                grid.Insert(i, _visiblePoints[i].Position);
        }

        PhotonPass.Execute(
            _scene,
            _visiblePoints,
            _pixels,
            grid,
            _config,
            token);

        RadiusUpdater.Update(_pixels, _config);

        FinalReconstruction(accumulation);

        GenerateDebugFrames();
    }

    private void FinalReconstruction(AccumulationBuffer accumulation)
    {
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                int idx = y * _width + x;

                ref var p = ref _pixels[idx];

                float area = MathF.PI * p.Radius * p.Radius;

                Vec3 indirect = area > 0f
                    ? p.Tau / area
                    : Vec3.Zero;

                accumulation.AddSample(
                    x,
                    y,
                    p.Direct + indirect);
            }
        }
    }

    private void GenerateDebugFrames()
    {
        _debugFrames.Clear();

        _debugFrames.Add(new DebugFrame
        {
            Name = "Photon Density",
            Width = _width,
            Height = _height,
            Pixels = new Vec3[_width * _height]
        });
    }

    public IReadOnlyList<DebugFrame> GetDebugFrames()
        => _debugFrames;

    public RenderStatistics GetStatistics()
        => _statistics;
}
