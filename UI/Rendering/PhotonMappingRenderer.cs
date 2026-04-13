using Core.Sampling;
using Engine.Integrators;
using Engine.PhotonMapping;
using Engine.PhotonMapping.DebugVisualization;
using Engine.Rendering;
using ScriptApi;

namespace UI.Rendering;

/// <summary>
/// Renders a scene using Progressive Photon Mapping with separate
/// accumulation of direct and indirect lighting components.
/// Direct lighting accumulates across passes (like path tracing).
/// Indirect lighting is re-estimated each pass with shrinking radius.
/// </summary>
public sealed class PhotonMappingRenderer
{
    public int TileSize { get; init; } = 16;

    // Preserved for debug visualization
    private PhotonMap? _lastPhotonMap;
    private PixelEstimationState[]? _lastPixelStates;
    private SceneDescription? _lastScene;

    public PhotonMap? LastPhotonMap => _lastPhotonMap;
    public PixelEstimationState[]? LastPixelStates => _lastPixelStates;

    public Task<FrameBuffer> RenderAsync(
        SceneDescription scene,
        Action<PhotonMappingProgress>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        _lastScene = scene;
        return Task.Run(
            () => Render(scene, onProgress, cancellationToken),
            cancellationToken);
    }

    private FrameBuffer Render(
        SceneDescription scene,
        Action<PhotonMappingProgress>? onProgress,
        CancellationToken cancellationToken)
    {
        var settings = scene.Integrator;
        var width = scene.Settings.ImageWidth;
        var height = scene.Settings.ImageHeight;

        // Both buffers accumulate across passes — mean is taken for display
        var directFb = new FrameBuffer(width, height);
        var indirectFb = new FrameBuffer(width, height);

        // Per-pixel PPM state
        var pixelStates = Enumerable
            .Range(0, width * height)
            .Select(_ => PixelEstimationState.Initial(settings.InitialRadius))
            .ToArray();

        var integrator = new PhotonMapIntegrator(
            settings.KNearest,
            settings.Alpha)
        {
            BackgroundRadiance = scene.Settings.BackgroundRadiance
        };

        var emitter = new PhotonEmitter();
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var pass = 0;
        var maxPasses = settings.MaxPasses == 0
            ? int.MaxValue
            : settings.MaxPasses;

        while (pass < maxPasses &&
               !cancellationToken.IsCancellationRequested)
        {
            pass++;

            // ── Emit photons ──────────────────────────────────────────────
            var photons = emitter.Emit(
                settings.PhotonsPerPass,
                scene.Scene,
                scene.Lights,
                cancellationToken: cancellationToken);

            if (cancellationToken.IsCancellationRequested) break;

            var photonMap = new PhotonMap(photons);
            _lastPhotonMap = photonMap;

            // ── Ray trace pass — direct + indirect separately ─────────────
            // Both buffers now accumulate across passes
            RayTracePass(scene, integrator, photonMap,
                         pixelStates, directFb, indirectFb,
                         pass,
                         cancellationToken);

            if (cancellationToken.IsCancellationRequested) break;

            _lastPixelStates = pixelStates.ToArray();

            // ── Combine for display ───────────────────────────────────────
            var combinedFb = CombineBuffers(directFb, indirectFb,
                                            width, height, pass);

            var avgRadius = pixelStates.Average(s => s.Radius);

            onProgress?.Invoke(new PhotonMappingProgress(
                pass,
                photons.Count,
                pass * (long)photons.Count,
                avgRadius,
                combinedFb,
                sw.Elapsed));
        }

        // Return final combined buffer
        return CombineBuffers(directFb, indirectFb, width, height, pass);
    }

    private void RayTracePass(
        SceneDescription scene,
        PhotonMapIntegrator integrator,
        PhotonMap photonMap,
        PixelEstimationState[] pixelStates,
        FrameBuffer directFb,
        FrameBuffer indirectFb,
        int pass,
        CancellationToken cancellationToken)
    {
        var width = scene.Settings.ImageWidth;
        var height = scene.Settings.ImageHeight;

        var tilesX = (int)Math.Ceiling((double)width / TileSize);
        var tilesY = (int)Math.Ceiling((double)height / TileSize);

        var tiles = new List<(int tx, int ty)>(tilesX * tilesY);
        for (var ty = 0; ty < tilesY; ty++)
            for (var tx = 0; tx < tilesX; tx++)
                tiles.Add((tx, ty));

        // Shuffle for pleasing reveal
        var rng = new Random();
        for (var i = tiles.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (tiles[i], tiles[j]) = (tiles[j], tiles[i]);
        }

        try
        {
            Parallel.ForEach(tiles,
                new ParallelOptions
                {
                    CancellationToken = cancellationToken
                },
                tile =>
                {
                    if (cancellationToken.IsCancellationRequested) return;

                    var (tx, ty) = tile;
                    var sampler = new Sampler(HashSeed(tx, ty, pass));

                    RenderTile(tx, ty, scene, integrator, photonMap,
                               pixelStates, directFb, indirectFb, sampler);
                });
        }
        catch (OperationCanceledException) { }
    }

    private void RenderTile(
        int tx, int ty,
        SceneDescription scene,
        PhotonMapIntegrator integrator,
        PhotonMap photonMap,
        PixelEstimationState[] pixelStates,
        FrameBuffer directFb,
        FrameBuffer indirectFb,
        Sampler sampler)
    {
        var width = scene.Settings.ImageWidth;
        var height = scene.Settings.ImageHeight;

        var x0 = tx * TileSize;
        var y0 = ty * TileSize;
        var x1 = Math.Min(x0 + TileSize, width);
        var y1 = Math.Min(y0 + TileSize, height);

        for (var y = y0; y < y1; y++)
            for (var x = x0; x < x1; x++)
            {
                var pixelIndex = y * width + x;
                var ray = scene.Camera.GenerateRay(
                    x, y,
                    sampler.Next(),
                    sampler.Next(),
                    sampler);

                // Direct lighting — accumulated across passes
                var direct = integrator.TraceDirect(
                    ray, scene.Scene, scene.Lights, sampler);
                directFb.AddSample(x, y, direct);

                // Indirect lighting — current pass only
                var indirect = integrator.TraceIndirect(
                    ray, scene.Scene, photonMap,
                    pixelStates, pixelIndex, sampler);
                indirectFb.AddSample(x, y, indirect);
            }
    }

    /// <summary>
    /// Combines direct (averaged across passes) and indirect (current pass)
    /// into a single display buffer.
    /// </summary>
    private static FrameBuffer CombineBuffers(
        FrameBuffer directFb,
        FrameBuffer indirectFb,
        int width, int height,
        int pass)
    {
        var combined = new FrameBuffer(width, height);

        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var direct = directFb.GetPixelRadiance(x, y);
                var indirect = indirectFb.GetPixelRadiance(x, y);
                combined.AddSample(x, y, direct + indirect);
            }

        return combined;
    }

    /// <summary>
    /// Creates a debug render context from the last rendered scene.
    /// </summary>
    public DebugRenderContext? GetDebugContext()
    {
        if (_lastScene is null) return null;

        return new DebugRenderContext(
            _lastScene.Camera,
            _lastScene.Scene,
            _lastScene.Lights,
            _lastScene.Settings.ImageWidth,
            _lastScene.Settings.ImageHeight,
            _lastScene.Settings.BackgroundRadiance,
            _lastScene.Integrator.KNearest,
            _lastScene.Integrator.Alpha);
    }

    private static int HashSeed(int tx, int ty, int pass = 0)
    {
        unchecked
        {
            var hash = (int)2166136261;
            hash = (hash ^ tx) * 16777619;
            hash = (hash ^ ty) * 16777619;
            hash = (hash ^ pass) * 16777619;
            return hash;
        }
    }
}