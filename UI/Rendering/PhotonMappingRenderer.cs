using Core.Sampling;
using Engine.Integrators;
using Engine.PhotonMapping;
using Engine.PhotonMapping.DebugVisualization;
using Engine.Rendering;
using ScriptApi;

namespace UI.Rendering;

/// <summary>
/// Renders a scene using Progressive Photon Mapping.
/// Each pass emits photons, estimates radiance and updates the frame buffer.
/// The radius shrinks each pass as per the PPM update rule (§3.11.4).
/// </summary>
public sealed class PhotonMappingRenderer
{
    /// <summary>Tile size in pixels for ray tracing passes.</summary>
    public int TileSize { get; init; } = 16;

    // Photon map and pixel states are preserved between passes
    // for debug visualization after rendering completes
    private PhotonMap? _lastPhotonMap;
    private PixelEstimationState[]? _lastPixelStates;
    private SceneDescription? _lastScene;

    /// <summary>The photon map from the last completed pass.</summary>
    public PhotonMap? LastPhotonMap => _lastPhotonMap;

    /// <summary>The pixel estimation states from the last completed pass.</summary>
    public PixelEstimationState[]? LastPixelStates => _lastPixelStates;

    /// <summary>
    /// Renders the scene using PPM, reporting progress after each pass.
    /// </summary>
    /// <param name="scene">The fully built scene description.</param>
    /// <param name="onProgress">
    /// Callback invoked after each PPM pass completes.
    /// Called on a thread pool thread — marshal to UI thread if needed.
    /// </param>
    /// <param name="cancellationToken">Token to cancel rendering.</param>
    /// <returns>The final frame buffer.</returns>
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
        var fb = new FrameBuffer(width, height);

        // Initialise per-pixel PPM state
        var pixelStates = Enumerable
            .Range(0, width * height)
            .Select(_ => PixelEstimationState.Initial(
                settings.InitialRadius))
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

        while (pass < maxPasses && !cancellationToken.IsCancellationRequested)
        {
            pass++;

            // ── Pass 1: Emit photons ──────────────────────────────────────
            var photons = emitter.Emit(
                settings.PhotonsPerPass,
                scene.Scene,
                scene.Lights,
                cancellationToken: cancellationToken);

            if (cancellationToken.IsCancellationRequested) break;

            // Build kd-tree sequentially after parallel emission
            var photonMap = new PhotonMap(photons);
            _lastPhotonMap = photonMap;

            // ── Pass 2: Ray trace + radiance estimation ───────────────────
            RayTracePass(scene, integrator, photonMap,
                         pixelStates, fb, cancellationToken);

            if (cancellationToken.IsCancellationRequested) break;

            // Store pixel states for debug visualization
            _lastPixelStates = pixelStates.ToArray();

            // Compute average radius for progress reporting
            var avgRadius = pixelStates
                .Average(s => s.Radius);

            onProgress?.Invoke(new PhotonMappingProgress(
                pass,
                photons.Count,
                pass * (long)photons.Count,
                avgRadius,
                fb,
                sw.Elapsed));
        }

        return fb;
    }

    private void RayTracePass(
        SceneDescription scene,
        PhotonMapIntegrator integrator,
        PhotonMap photonMap,
        PixelEstimationState[] pixelStates,
        FrameBuffer frameBuffer,
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

        // Shuffle tiles for pleasing progressive reveal
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
                    var sampler = new Sampler(HashSeed(tx, ty));

                    RenderTile(tx, ty, scene, integrator, photonMap,
                               pixelStates, frameBuffer, sampler);
                });
        }
        catch (OperationCanceledException)
        {
            // Return partial results
        }
    }

    private void RenderTile(
        int tx, int ty,
        SceneDescription scene,
        PhotonMapIntegrator integrator,
        PhotonMap photonMap,
        PixelEstimationState[] pixelStates,
        FrameBuffer frameBuffer,
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

                var radiance = integrator.Trace(
                    ray,
                    scene.Scene,
                    scene.Lights,
                    photonMap,
                    pixelStates,
                    pixelIndex,
                    sampler);

                frameBuffer.AddSample(x, y, radiance);
            }
    }

    private static int HashSeed(int tx, int ty)
    {
        unchecked
        {
            var hash = (int)2166136261;
            hash = (hash ^ tx) * 16777619;
            hash = (hash ^ ty) * 16777619;
            return hash;
        }
    }

    /// <summary>
    /// Creates a debug render context from the last rendered scene.
    /// Returns null if no scene has been rendered yet.
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
}