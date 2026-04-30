using System.Text;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Camera;
using Core.Lights;
using Core.Materials;
using Core.Math;
using Core.Rendering;
using Core.Scene;

namespace Studio;

public partial class MainViewModel : ObservableObject
{
    private readonly SynchronizationContext _ui;

    private CancellationTokenSource? _externalCts; // optional: if you also want manual CTS control

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
@"scene: CornellMaterialsShowcase
camera: pinhole
notes:
- Later we will make this a real script. For now, these lines select built-in options.";

        StatusText = "Idle";
        StatsText = "";
    }

    // Bindable UI properties
    [ObservableProperty] private ImageSource? renderImage;
    [ObservableProperty] private string statsText = "";
    [ObservableProperty] private string statusText = "";

    [ObservableProperty] private string widthInput = "640";
    [ObservableProperty] private string heightInput = "360";
    [ObservableProperty] private string tileSizeInput = "16";
    [ObservableProperty] private string threadsInput = "";
    [ObservableProperty] private string sceneScript = "";

    // A derived property to enable/disable Start in XAML if you want it:
    public bool CanStart => !StartRenderCommand.IsRunning; // IAsyncRelayCommand exposes IsRunning [4](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/asyncrelaycommand)

    partial void OnStatusTextChanged(string value)
    {
        // optional hook for logging or other
    }

    // Start command (async) with built-in cancellation support.
    // IncludeCancelCommand generates StartRenderCancelCommand automatically. [6](https://learn.microsoft.com/en-us/dotnet/api/communitytoolkit.mvvm.input.relaycommandattribute.includecancelcommand?view=dotnet-comm-toolkit-8.4)[3](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/generators/relaycommand)
    [RelayCommand(IncludeCancelCommand = true)]
    private async Task StartRenderAsync(CancellationToken token)
    {
        StatusText = "Rendering...";

        int width = ParseInt(WidthInput, 640);
        int height = ParseInt(HeightInput, 360);
        int tileSize = ParseInt(TileSizeInput, 16);
        int? threads = string.IsNullOrWhiteSpace(ThreadsInput) ? null : ParseInt(ThreadsInput, Environment.ProcessorCount);

        _accum = new AccumulationBuffer(width, height);

        _presenter ??= new WriteableBitmapPresenter(width, height);
        _presenter.Resize(width, height);
        RenderImage = _presenter.Bitmap;

        var (scene, camera) = BuildSceneFromScript(width, height);

        // Progressive render loop: update tiles as they finish.
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
            progressEveryNTiles: 8);

        StatusText = "Stopped";
    }

    // Optional explicit Stop command, if you want a separate Stop button binding.
    // If you use IncludeCancelCommand, you can bind Stop to StartRenderCancelCommand instead. [6](https://learn.microsoft.com/en-us/dotnet/api/communitytoolkit.mvvm.input.relaycommandattribute.includecancelcommand?view=dotnet-comm-toolkit-8.4)
    [RelayCommand]
    private void StopRender()
    {
        if (StartRenderCommand.IsRunning)
        {
            StartRenderCancelCommand.Execute(null);
            StatusText = "Cancelling...";
        }
    }

    private void PostStats(RenderProgress p)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Resolution: {p.Width} x {p.Height}");
        sb.AppendLine($"SPP min/max: {p.SamplesPerPixelMin} / {p.SamplesPerPixelMax}");
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

    private (Scene scene, ICamera camera) BuildSceneFromScript(int width, int height)
    {
        string sceneName = "CornellMaterialsShowcase";
        string cameraName = "pinhole";

        foreach (var raw in SceneScript.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
        {
            var line = raw.Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            if (line.StartsWith("scene:", StringComparison.OrdinalIgnoreCase))
                sceneName = line.Substring("scene:".Length).Trim();
            else if (line.StartsWith("camera:", StringComparison.OrdinalIgnoreCase))
                cameraName = line.Substring("camera:".Length).Trim();

            if (sceneName.Length > 0 && cameraName.Length > 0) break;
        }

        // Minimal built-in scene (keep it simple for milestone start)
        float aspect = (float)width / height;

        var red = new Lambertian(new Vec3(0.65f, 0.05f, 0.05f));
        var green = new Lambertian(new Vec3(0.12f, 0.45f, 0.15f));
        var white = new Lambertian(new Vec3(0.73f, 0.73f, 0.73f));
        var lightRadiance = new Vec3(15f, 15f, 15f);
        var lightMat = new DiffuseLight(lightRadiance);

        var list = new HittableList();
        list.Add(new YZRect(0, 555, 0, 555, 555, green));
        list.Add(new YZRect(0, 555, 0, 555, 0, red));
        list.Add(new XZRect(0, 555, 0, 555, 0, white));
        list.Add(new XZRect(0, 555, 0, 555, 555, white));
        list.Add(new XYRect(0, 555, 0, 555, 555, white));
        list.Add(new FlipFace(new XZRect(213, 343, 227, 332, 554, lightMat)));

        var metal = new MicrofacetMetal(new Vec3(0.95f, 0.93f, 0.88f), roughness: 0.25f);
        var glass = new Dielectric(ior: 1.5f);
        list.Add(new Sphere(new Vec3(190f, 90f, 190f), 90f, metal));
        list.Add(new Sphere(new Vec3(370f, 90f, 370f), 90f, glass));

        var world = new BvhNode(list.Objects);

        var lights = new List<ILight>
        {
            new RectAreaLightXZ(213, 343, 227, 332, 554, normal: -Vec3.UnitY, radiance: lightRadiance)
        };

        var scene = new Scene(world, lights);

        var lookFrom = new Vec3(278f, 278f, -800f);
        var lookAt = new Vec3(278f, 278f, 0f);

        ICamera camera =
            string.Equals(cameraName, "thinlens", StringComparison.OrdinalIgnoreCase)
                ? new ThinLensCamera(40f, aspect, lookFrom, lookAt, Vec3.UnitY,
                    focusDistance: (lookAt - lookFrom).Length(), apertureRadius: 0.2f)
                : new PinholeCamera(40f, aspect, lookFrom, lookAt, Vec3.UnitY);

        return (scene, camera);
    }

    private static int ParseInt(string s, int fallback)
        => int.TryParse(s, out var v) ? v : fallback;
}