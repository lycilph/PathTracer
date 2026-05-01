using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Camera;
using Core.Debugging;
using Core.Math;
using Core.PhotonMapping;
using Core.Rendering;
using Core.Scene;
using Scripting;

namespace Studio;

public partial class MainViewModel : ObservableObject
{
    private readonly SynchronizationContext _ui;
    private readonly ISceneScriptEngine _scriptEngine = new RoslynSceneScriptEngine();

    private WriteableBitmapPresenter? _presenter;
    private AccumulationBuffer? _accum;

    private CancellationTokenSource? _compileDebounceCts;

    private DebugBufferSet? _debugBuffers;
    private int _debugIteration;
    private int _sppmIteration;
    private float _depthInvMax = 1f;
    private readonly object _debugLock = new();
    private ICamera? _lastCamera;
    private Scene? _lastScene;


    public event Action<int, int>? GoToRequested; // (line, column)

    public MainViewModel()
    {
        // Capture UI context for marshaling updates without referencing Application.Current in core logic.
        _ui = SynchronizationContext.Current ?? new SynchronizationContext();

        WidthInput = "400";
        HeightInput = "400";
        TileSizeInput = "16";
        ThreadsInput = "";

        SceneScript =
@"// Return a SceneDefinition.
// Globals: Width, Height, Scene (SceneApi)
return Scene.CornellSimple(thinLens: false);
//return Scene.CornellDefault(tintedGlass: true);
//return Scene.DOFDefault(thinLens: false);
//return Scene.MotionBlurDefault(thinLens: false);";

        StatusText = "Idle";
        StatsText = "";
    }

    // Bindable UI properties
    [ObservableProperty] private ImageSource? renderImage;
    [ObservableProperty] private string statsText = "";
    [ObservableProperty] private string statusText = "";

    [ObservableProperty] private string widthInput = "400";
    [ObservableProperty] private string heightInput = "400";
    [ObservableProperty] private string targetSppInput = "256";
    [ObservableProperty] private string tileSizeInput = "16";
    [ObservableProperty] private string threadsInput = "";
    [ObservableProperty] private string sceneScript = "";

    [ObservableProperty] private int progressPercentage = 0;
    [ObservableProperty] private bool progressIndeterminate = false;


    [ObservableProperty] private string selectedDebugBuffer = "Beauty";
    public string[] DebugBufferNames { get; } =
    {
        "Beauty",
        "Depth",
        "Normal",
        "Albedo",
        "VisiblePointMask",
        "Throughput",
        "PhotonHitMapXZ",
        "PhotonFluxMapXZ",
        // Placerholders below this
        "Radius",
        "PhotonCountN",
        "PhotonCountM",
        "IndirectPhoton"
    };

    [ObservableProperty]
    private ObservableCollection<ScriptDiagnosticItem> scriptDiagnostics = [];


    // A derived property to enable/disable Start in XAML if you want it:
    public bool CanStart => !StartRenderCommand.IsRunning; // IAsyncRelayCommand exposes IsRunning [4](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/asyncrelaycommand)

    partial void OnStatusTextChanged(string value)
    {
        // optional hook for logging or other
    }

    partial void OnSceneScriptChanged(string value)
    {
        _compileDebounceCts?.Cancel();
        _compileDebounceCts = new CancellationTokenSource();
        var token = _compileDebounceCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(300, token); // debounce
                CompileScriptForDiagnostics(value);
            }
            catch (OperationCanceledException) { }
        });
    }

    partial void OnSelectedDebugBufferChanged(string value)
    {
        if (_debugBuffers == null || _lastScene == null || _lastCamera == null) return;

        if (value != "Beauty")
        {
            RefreshEyeDebug(_debugBuffers.Width, _debugBuffers.Height, _lastCamera, _lastScene);
        }

        PostTileUpdate(0, 0, _debugBuffers.Width, _debugBuffers.Height);
    }

    [RelayCommand]
    private void GoToDiagnostic(ScriptDiagnosticItem? diag)
    {
        if (diag is null) return;
        GoToRequested?.Invoke(diag.Line, diag.Column);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task StartRenderAsync(CancellationToken token)
    {
        StatusText = "Rendering...";

        int width = ParseInt(WidthInput, 640);
        int height = ParseInt(HeightInput, 360);
        int tileSize = ParseInt(TileSizeInput, 16);
        int? threads = string.IsNullOrWhiteSpace(ThreadsInput) ? null : ParseInt(ThreadsInput, Environment.ProcessorCount);

        int targetSppParsed = ParseInt(TargetSppInput, 0);
        int? targetSpp = targetSppParsed > 0 ? targetSppParsed : null;

        if (targetSpp == null)
            ProgressIndeterminate = true;
        else
            ProgressPercentage = 0;

        _accum = new AccumulationBuffer(width, height);

        _presenter ??= new WriteableBitmapPresenter(width, height);
        _presenter.Resize(width, height);

        // Set RenderImage on UI thread before we start background work
        RenderImage = _presenter.Bitmap;


        var built = await TryBuildSceneFromScriptAsync(width, height, token);
        if (built is null) return;

        var (scene, camera) = built.Value;
        _lastCamera = camera;
        _lastScene = scene;

        try
        {
            // Important: move the long-running progressive loop off the UI thread
            await Task.Run(async () =>
            {
                _debugBuffers = new DebugBufferSet(width, height);
                _debugIteration = 0;
                _depthInvMax = 1f;

                // Fill debug buffers once initially
                RefreshEyeDebug(width, height, camera, scene);

                await ProgressiveRenderer.RenderLoopAsync(
                    width: width,
                    height: height,
                    camera: camera,
                    scene: scene,
                    accum: _accum,
                    tileSize: tileSize,
                    baseSeed: 123,
                    token: token,
                    reportProgress: p => PostStats(p),
                    reportTileUpdated: (x0, y0, w, h) => PostTileUpdate(x0, y0, w, h),
                    maxDegreeOfParallelism: threads,
                    progressEveryNTiles: 8,
                    targetSpp: targetSpp);
            }, token);
        }
        catch (OperationCanceledException)
        {
            StatusText = "Stopped";
        }
        catch (Exception ex)
        {
            StatusText = "Error";
            StatsText = ex.ToString();
        }
        finally
        {
            // optional: set final status
            if (!token.IsCancellationRequested)
                StatusText = targetSpp.HasValue ? $"Finished (Target SPP {targetSpp.Value})" : "Finished";

            ProgressIndeterminate = false;
            ProgressPercentage = 0;
        }
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task StartSppmDebugAsync(CancellationToken token)
    {
        StatusText = "SPPM (Photon Debug) running...";

        await Task.Run(() =>
        {
            while (!token.IsCancellationRequested)
            {
                RunPhotonDebugIteration();
            }
        }, token);

        StatusText = "Stopped";
    }

    private void RunPhotonDebugIteration()
    {
        if (_debugBuffers == null || _lastScene == null || _lastCamera == null)
            return;

        // 1) Eye pass debug (optional each iteration; you can keep it only on view change if you want)
        RefreshEyeDebug(_debugBuffers.Width, _debugBuffers.Height, _lastCamera, _lastScene);

        // 2) Photon pass
        var stats = new PhotonTraceStats();
        var photons = PhotonTracer.TracePhotonPass(
            scene: _lastScene,
            photonsPerPass: 1_000_000,      // relaxed time constraints; tweak later
            maxDepth: 12,
            baseSeed: 12345,
            iterationIndex: _sppmIteration++,
            stats: stats);

        // 3) Fill photon debug images
        lock (_debugLock)
        {
            PhotonDebugImages.FillPhotonMapsXZ(
                _lastScene,
                photons,
                _debugBuffers);
        }

        // 4) Update stats text (optional)
        _ui.Post(_ =>
        {
            StatsText =
                $"SPPM 12.1 Photon Pass\n" +
                $"Photons requested: {stats.PhotonsRequested}\n" +
                $"Photons emitted:   {stats.PhotonsEmitted}\n" +
                $"Photons stored:    {stats.PhotonsStored} (Lambertian only)\n" +
                $"Avg path length:   {stats.AvgPathLength:0.00}\n" +
                $"RR terminated:     {stats.PathsTerminatedRR}\n" +
                $"MaxDepth term:     {stats.PathsTerminatedMaxDepth}\n";
        }, null);

        // 5) Force repaint (your proven approach)
        PostTileUpdate(0, 0, _debugBuffers.Width, _debugBuffers.Height);
    }

    private void PostStats(RenderProgress p)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Resolution: {p.Width} x {p.Height}");
        sb.AppendLine($"SPP min/max: {p.SamplesPerPixelMin} / {p.SamplesPerPixelMax}");

        if (ParseInt(TargetSppInput, 0) > 0)
        {
            int target = ParseInt(TargetSppInput, 0);
            sb.AppendLine($"Target SPP: {target}  (min {p.SamplesPerPixelMin})");

            ProgressPercentage = (p.SamplesPerPixelMin * 100) / target;
        }

        sb.AppendLine($"Tiles: {p.TilesDone} / {p.TilesTotal}");
        sb.AppendLine($"Elapsed: {p.ElapsedSeconds:0.0}s");
        sb.AppendLine($"Samples/sec: {p.SamplesPerSecond:0}");
        sb.AppendLine($"ms/tile: {p.MsPerTile:0.0}");
        sb.AppendLine($"Avg luminance: {p.AverageLuminance:0.000}");

        _ui.Post(_ => StatsText = sb.ToString(), null);
    }

    private void PostTileUpdate(int x0, int y0, int w, int h)
    {
        if (_accum is null || _presenter is null) return;

        _ui.Post(_ =>
        {
            if (SelectedDebugBuffer == "Beauty")
            {
                _presenter!.UpdateTile(_accum!, x0, y0, w, h);
            }
            else
            {
                lock (_debugLock)
                {
                    var buf = GetActiveDebugBuffer();
                    var width = _debugBuffers!.Width;
                    var height = _debugBuffers!.Height;

                    Func<Vec3, Vec3>? transform = null;
                    if (SelectedDebugBuffer == "Depth")
                    {
                        float invMax;
                        lock (_debugLock) invMax = _depthInvMax;

                        transform = v =>
                        {
                            float d = 1f - MathUtil.Clamp(v.X * invMax, 0f, 1f); // closer = brighter
                            return new Vec3(d, d, d);
                        };
                    }

                    _presenter!.UpdateTileFromRawRgb(buf, width, height, x0, y0, w, h, transform: transform);
                }

            }
        }, null);
    }

    private async Task<(Scene scene, ICamera camera)?> TryBuildSceneFromScriptAsync(int width, int height, CancellationToken token)
    {
        var compile = _scriptEngine.TryCompile(SceneScript);

        if (!compile.Success)
        {
            StatsText = (compile.ErrorText ?? "Compilation failed") + Environment.NewLine
                        + string.Join(Environment.NewLine, compile.Diagnostics);
            StatusText = "Script error";
            return null;
        }

        // Optional: show warnings/info in stats
        if (compile.Diagnostics.Count > 0)
            StatsText = string.Join(Environment.NewLine, compile.Diagnostics);

        try
        {
            var def = await _scriptEngine.ExecuteAsync(SceneScript, width, height, token);
            return (def.Scene, def.Camera);
        }
        catch (Exception ex)
        {
            StatsText = ex.ToString();
            StatusText = "Script runtime error";
            return null;
        }
    }

    private static int ParseInt(string s, int fallback)
        => int.TryParse(s, out var v) ? v : fallback;

    private void CompileScriptForDiagnostics(string code)
    {
        var result = _scriptEngine.TryCompile(code);

        var items = result.Diagnostics.Select(d => new ScriptDiagnosticItem
        {
            Severity = d.Severity.ToString(),
            Id = d.Id,
            Line = d.Line,
            Column = d.Column,
            Message = d.Message,

            StartOffset = d.SpanStart,
            Length = d.SpanLength
        }).ToList();

        _ui.Post(_ =>
        {
            ScriptDiagnostics.Clear();
            foreach (var it in items)
                ScriptDiagnostics.Add(it);
        }, null);
    }

    private Vec3[] GetActiveDebugBuffer()
    {
        if (_debugBuffers is null)
            throw new InvalidOperationException("Debug buffers not initialized.");

        return SelectedDebugBuffer switch
        {
            "Beauty" => null!, // handled separately (beauty comes from accumulation)
            "Depth" => _debugBuffers.Get(DebugBufferId.Depth),
            "Normal" => _debugBuffers.Get(DebugBufferId.Normal),
            "Albedo" => _debugBuffers.Get(DebugBufferId.Albedo),
            "VisiblePointMask" => _debugBuffers.Get(DebugBufferId.VisiblePointMask),
            "Throughput" => _debugBuffers.Get(DebugBufferId.Throughput),
            "PhotonHitMapXZ" => _debugBuffers!.Get(DebugBufferId.PhotonHitMapXZ),
            "PhotonFluxMapXZ" => _debugBuffers!.Get(DebugBufferId.PhotonFluxMapXZ),
            "Radius" => _debugBuffers.Get(DebugBufferId.Radius),
            "PhotonCountN" => _debugBuffers.Get(DebugBufferId.PhotonCountN),
            "PhotonCountM" => _debugBuffers.Get(DebugBufferId.PhotonCountM),
            "IndirectPhoton" => _debugBuffers.Get(DebugBufferId.IndirectPhoton),
            _ => null!
        };
    }

    private void RefreshEyeDebug(int width, int height, ICamera camera, Scene scene)
    {
        if (_debugBuffers is null) return;

        lock (_debugLock)
        {
            EyePassDebugger.Fill(width, height, camera, scene, _debugBuffers!, baseSeed: 999, iterationIndex: _debugIteration++);

            _depthInvMax = ComputeDepthInvMax(_debugBuffers);
        }
    }

    private static float ComputeDepthInvMax(DebugBufferSet dbg)
    {
        var depth = dbg.Get(DebugBufferId.Depth);

        float max = 0f;
        for (int i = 0; i < depth.Length; i++)
        {
            float d = depth[i].X;
            if (d > max && d < 1e30f) // ignore absurd values just in case
                max = d;
        }

        if (max <= 1e-6f) max = 1f;
        return 1f / max;
    }
}