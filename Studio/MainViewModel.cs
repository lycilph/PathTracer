using System.Text;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Camera;
using Core.Rendering;
using Core.Scene;
using Scripting;

namespace Studio;

public partial class MainViewModel : ObservableObject
{
    private readonly SynchronizationContext _ui;
    private readonly ISceneScriptEngine _scriptEngine = new RoslynSceneScriptEngine();

    //private CancellationTokenSource? _externalCts; // optional: if you also want manual CTS control

    private WriteableBitmapPresenter? _presenter;
    private AccumulationBuffer? _accum;

    public MainViewModel()
    {
        // Capture UI context for marshaling updates without referencing Application.Current in core logic.
        _ui = SynchronizationContext.Current ?? new SynchronizationContext();

        WidthInput = "640";
        HeightInput = "360";
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

    [ObservableProperty] private string widthInput = "640";
    [ObservableProperty] private string heightInput = "360";
    [ObservableProperty] private string targetSppInput = "256";
    [ObservableProperty] private string tileSizeInput = "16";
    [ObservableProperty] private string threadsInput = "";
    [ObservableProperty] private string sceneScript = "";

    [ObservableProperty] private int progressPercentage = 0;
    [ObservableProperty] private bool progressIndeterminate = false;

    // A derived property to enable/disable Start in XAML if you want it:
    public bool CanStart => !StartRenderCommand.IsRunning; // IAsyncRelayCommand exposes IsRunning [4](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/asyncrelaycommand)

    partial void OnStatusTextChanged(string value)
    {
        // optional hook for logging or other
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
}