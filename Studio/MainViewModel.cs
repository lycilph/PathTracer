using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Camera;
using Core.Rendering;
using Core.Rendering.Sppm;
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
return Scene.SppmCaustic();
//return Scene.CornellSimple(thinLens: false);
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


    [ObservableProperty]
    private ObservableCollection<ScriptDiagnosticItem> scriptDiagnostics = [];
    /// <summary>Currently selected renderer.  Bound to the Mode combo-box in XAML.</summary>
    public string RendererMode
    {
        get => _rendererMode;
        set
        {
            if (SetProperty(ref _rendererMode, value))
                OnPropertyChanged(nameof(IsSppmMode));
        }
    }

    private string _rendererMode = RendererModes.PathTracer;

    /// <summary>True when SPPM is selected; drives Visibility of SPPM parameter panel.</summary>
    public bool IsSppmMode => _rendererMode == RendererModes.Sppm;

    /// <summary>Photons emitted per SPPM iteration (parsed from UI text-box).</summary>
    public string PhotonsPerIterInput
    {
        get => _photonsPerIterInput;
        set => SetProperty(ref _photonsPerIterInput, value);
    }
    private string _photonsPerIterInput = "200000";

    /// <summary>Initial photon search radius (scene-units).</summary>
    public string SppmRadiusInput
    {
        get => _sppmRadiusInput;
        set => SetProperty(ref _sppmRadiusInput, value);
    }
    private string _sppmRadiusInput = "10";

    /// <summary>SPPM alpha – controls the rate of radius reduction (0 &lt; α ≤ 1).</summary>
    public string SppmAlphaInput
    {
        get => _sppmAlphaInput;
        set => SetProperty(ref _sppmAlphaInput, value);
    }
    private string _sppmAlphaInput = "0.7";

    /// <summary>Renderer-mode constants to avoid magic strings.</summary>
    public static class RendererModes
    {
        public const string PathTracer = "PathTracer";
        public const string Sppm = "SPPM";
    }

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

    [RelayCommand]
    private void GoToDiagnostic(ScriptDiagnosticItem? diag)
    {
        if (diag is null) return;
        GoToRequested?.Invoke(diag.Line, diag.Column);
    }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task StartRenderAsync(CancellationToken token)
    {
        int width = ParseInt(WidthInput, 640);
        int height = ParseInt(HeightInput, 360);
        int? threads = string.IsNullOrWhiteSpace(ThreadsInput) ? null : ParseInt(ThreadsInput, Environment.ProcessorCount);

        _accum = new AccumulationBuffer(width, height);
        
        _presenter ??= new WriteableBitmapPresenter(width, height);
        _presenter.Resize(width, height);
        // Set RenderImage on UI thread before we start background work
        RenderImage = _presenter.Bitmap;

        var built = await TryBuildSceneFromScriptAsync(width, height, token);
        if (built is null) return;

        var (scene, camera) = built.Value;

        if (RendererMode == RendererModes.Sppm)
        {
            await RunSppmAsync(width, height, camera, scene, threads, token);
        }
        else
        {
            await RunPathTracerAsync(width, height, camera, scene, threads, token);
        }

        //StatusText = "Rendering...";

        //int width = ParseInt(WidthInput, 640);
        //int height = ParseInt(HeightInput, 360);
        //int tileSize = ParseInt(TileSizeInput, 16);
        //int? threads = string.IsNullOrWhiteSpace(ThreadsInput) ? null : ParseInt(ThreadsInput, Environment.ProcessorCount);

        //int targetSppParsed = ParseInt(TargetSppInput, 0);
        //int? targetSpp = targetSppParsed > 0 ? targetSppParsed : null;

        //if (targetSpp == null)
        //    ProgressIndeterminate = true;
        //else
        //    ProgressPercentage = 0;

        //_accum = new AccumulationBuffer(width, height);

        //_presenter ??= new WriteableBitmapPresenter(width, height);
        //_presenter.Resize(width, height);

        //// Set RenderImage on UI thread before we start background work
        //RenderImage = _presenter.Bitmap;


        //var built = await TryBuildSceneFromScriptAsync(width, height, token);
        //if (built is null) return;

        //var (scene, camera) = built.Value;

        //try
        //{
        //    // Important: move the long-running progressive loop off the UI thread
        //    await Task.Run(async () =>
        //    {
        //        await ProgressiveRenderer.RenderLoopAsync(
        //            width: width,
        //            height: height,
        //            camera: camera,
        //            scene: scene,
        //            accum: _accum,
        //            tileSize: tileSize,
        //            baseSeed: 123,
        //            token: token,
        //            reportProgress: p => PostStats(p),
        //            reportTileUpdated: (x0, y0, w, h) => PostTileUpdate(x0, y0, w, h),
        //            maxDegreeOfParallelism: threads,
        //            progressEveryNTiles: 8,
        //            targetSpp: targetSpp);
        //    }, token);
        //}
        //catch (OperationCanceledException)
        //{
        //    StatusText = "Stopped";
        //}
        //catch (Exception ex)
        //{
        //    StatusText = "Error";
        //    StatsText = ex.ToString();
        //}
        //finally
        //{
        //    // optional: set final status
        //    if (!token.IsCancellationRequested)
        //        StatusText = targetSpp.HasValue ? $"Finished (Target SPP {targetSpp.Value})" : "Finished";

        //    ProgressIndeterminate = false;
        //    ProgressPercentage = 0;
        //}
    }

    private async Task RunSppmAsync(
        int width, int height,
        ICamera camera, Scene scene,
        int? threads, CancellationToken token)
    {
        int photons = ParseInt(PhotonsPerIterInput, SppmRenderer.DefaultPhotonsPerIteration);
        float radius = ParseFloat(SppmRadiusInput, SppmRenderer.DefaultInitialRadius);
        float alpha = ParseFloat(SppmAlphaInput, SppmRenderer.DefaultAlpha);

        // Clamp / validate
        photons = Math.Max(1_000, photons);
        radius = Math.Max(0.001f, radius);
        alpha = Math.Clamp(alpha, 0.01f, 1f);

        var pixelStates = SppmRenderer.CreatePixelStates(width, height, radius);

        StatusText = "SPPM starting…";

        try
        {
            await Task.Run(async () =>
            {
                await SppmRenderer.RenderLoopAsync(
                    width, height, camera, scene,
                    pixelStates,
                    photonsPerIteration: photons,
                    alpha: alpha,
                    token: token,
                    reportProgress: progress =>
                    {
                        // Marshal back to UI thread
                        App.Current.Dispatcher.BeginInvoke(() =>
                        {
                            StatsText = progress.FormatStats();
                            // Reuse the existing progress bar: show radius reduction as a
                            // proxy for "convergence" (from 1 → 0 conceptually, so invert).
                            ProgressPercentage = (int)(Math.Clamp(
                                1.0 - progress.AverageRadius / radius, 0.0, 1.0) * 100);
                        });
                    },
                    reportFrame: frame =>
                    {
                        App.Current.Dispatcher.BeginInvoke(() =>
                            _presenter.UpdateFullFrame(frame));
                    },
                    maxDegreeOfParallelism: threads);
            }, token);
        }
        catch (OperationCanceledException) { /* normal stop */ }
        finally
        {
            StatusText = "Stopped";
        }
    }

    private async Task RunPathTracerAsync(
        int width, int height,
        ICamera camera, Scene scene,
        int? threads, CancellationToken token)
    {
        StatusText = "Rendering...";
        
        int tileSize = ParseInt(TileSizeInput, 16);

        int targetSppParsed = ParseInt(TargetSppInput, 0);
        int? targetSpp = targetSppParsed > 0 ? targetSppParsed : null;

        if (targetSpp == null)
            ProgressIndeterminate = true;
        else
            ProgressPercentage = 0;

        try
        {
            // Important: move the long-running progressive loop off the UI thread
            await Task.Run(async () =>
            {
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
            _presenter.UpdateTile(_accum, x0, y0, w, h);
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

    private static float ParseFloat(string s, float fallback)
        => float.TryParse(s,
               System.Globalization.NumberStyles.Float,
               System.Globalization.CultureInfo.InvariantCulture,
               out float v) ? v : fallback;

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
}