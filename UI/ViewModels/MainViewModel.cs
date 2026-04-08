using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Engine.PhotonMapping.Debug;
using Engine.PhotonMapping.DebugVisualization;
using Engine.Rendering;
using ScriptApi;
using ScriptApi.Validation;
using UI.Rendering;
using UI.Scripting;
using UI.Services;

namespace UI.ViewModels;

/// <summary>
/// Main view model — owns all application state and commands.
/// </summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly ScriptCompiler _compiler = new();
    private CancellationTokenSource? _cts;
    private string? _currentFilePath;
    private DateTime _lastBitmapUpdate = DateTime.MinValue;
    private readonly RecentFilesService _recentFilesService = new();
    private IReadOnlyList<ScriptError> _lastScriptErrors = [];
    private PhotonMappingRenderer? _photonRenderer;
    private FrameBuffer? _frameBuffer;

    // Statistics
    private int _totalTiles;
    private int _tileSize;
    private int _samplesPerPixel;
    private DateTime _renderStartTime;

    // ── Script editor ─────────────────────────────────────────────────────────

    [ObservableProperty]
    private string _scriptText = DefaultScript;

    public IReadOnlyList<ScriptError> LastScriptErrors => _lastScriptErrors;

    // ── Status ────────────────────────────────────────────────────────────────

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private double _progressPercent;

    [ObservableProperty]
    private string _progressText = string.Empty;

    [ObservableProperty]
    private bool _isRendering;
    
    [ObservableProperty]
    private bool _isWarmingUp = true;

    [ObservableProperty]
    private string _warmupStatusText = "Warming up...";

    // ── Render statistics ─────────────────────────────────────────────────────

    [ObservableProperty]
    private string _resolutionText = string.Empty;

    [ObservableProperty]
    private string _samplesText = string.Empty;

    [ObservableProperty]
    private string _elapsedText = string.Empty;

    [ObservableProperty]
    private string _tilesText = string.Empty;

    [ObservableProperty]
    private string _tilesPerSecText = string.Empty;

    [ObservableProperty]
    private string _raysPerSecText = string.Empty;

    // ── Photon mapping stats ──────────────────────────────────────────────────

    [ObservableProperty]
    private bool _isPhotonMapping;

    [ObservableProperty]
    private string _currentPassText = string.Empty;

    [ObservableProperty]
    private string _totalPhotonsText = string.Empty;

    [ObservableProperty]
    private string _averageRadiusText = string.Empty;

    // ── Scene info ────────────────────────────────────────────────────────────

    [ObservableProperty]
    private string _primitiveCountText = string.Empty;

    [ObservableProperty]
    private string _lightCountText = string.Empty;

    [ObservableProperty]
    private string _acceleratorTypeText = string.Empty;

    // ── Debug visualization ───────────────────────────────────────────────────

    private readonly PhotonDebugRenderer _debugRenderer = new();

    [ObservableProperty]
    private PhotonDebugMode _selectedDebugMode = PhotonDebugMode.None;

    [ObservableProperty]
    private bool _canApplyDebugView;

    public IReadOnlyList<PhotonDebugMode> AvailableDebugModes { get; } =
        Enum.GetValues<PhotonDebugMode>().ToList();

    // ── Image saving ──────────────────────────────────────────────────────────

    [ObservableProperty]
    private bool _canSaveImage;

    // ── Recent files ──────────────────────────────────────────────────────────

    [ObservableProperty]
    private ObservableCollection<string> _recentFiles = [];

    // ── Preview image ─────────────────────────────────────────────────────────

    [ObservableProperty]
    private WriteableBitmap? _previewBitmap;

    // ── Validation messages ───────────────────────────────────────────────────

    public ObservableCollection<ValidationMessage> ValidationMessages { get; } = [];

    // ── Window title ──────────────────────────────────────────────────────────

    [ObservableProperty]
    private string _windowTitle = "PathTracer";

    public MainViewModel()
    {
        LoadRecentFiles();
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RunAsync()
    {
        IsRendering = true;
        StatusText = "Compiling script...";
        ValidationMessages.Clear();
        _cts = new CancellationTokenSource();
        RunCommand.NotifyCanExecuteChanged();
        AbortCommand.NotifyCanExecuteChanged();

        try
        {
            // ── Compile ───────────────────────────────────────────────────────
            var scriptResult = await _compiler.CompileAndRunAsync(
                ScriptText, _cts.Token);

            if (!scriptResult.IsSuccess)
            {
                _lastScriptErrors = scriptResult.Errors;
                foreach (var error in scriptResult.Errors)
                    ValidationMessages.Add(new ValidationMessage(
                        ValidationSeverity.Error, 
                        error.ToString()));

                StatusText = $"Compilation failed — " +
                             $"{scriptResult.Errors.Count} error(s)";
                OnPropertyChanged(nameof(LastScriptErrors));
                return;
            }

            // Clear errors on success
            _lastScriptErrors = [];
            OnPropertyChanged(nameof(LastScriptErrors));

            var scene = scriptResult.Scene!;

            // ── Validation ────────────────────────────────────────────────────
            foreach (var message in scene.Validation.Messages)
                ValidationMessages.Add(message);

            if (!scene.Validation.IsValid)
            {
                StatusText = "Scene validation failed — see errors below";
                return;
            }

            // ── Setup bitmap ──────────────────────────────────────────────────
            PreviewBitmap = new WriteableBitmap(
                scene.Settings.ImageWidth,
                scene.Settings.ImageHeight,
                96, 96,
                System.Windows.Media.PixelFormats.Rgb24,
                null);

            StatusText = "Rendering...";

            // ── Render ────────────────────────────────────────────────────────

            // ── Populate scene info ───────────────────────────────────────────────────
            PrimitiveCountText = scene.PrimitiveCount.ToString();
            LightCountText = scene.Lights.Count.ToString();
            AcceleratorTypeText = scene.Scene.GetType().Name;
            ResolutionText = $"{scene.Settings.ImageWidth} × {scene.Settings.ImageHeight}";
            SamplesText = scene.Integrator.Type == IntegratorType.PathTracing
                ? $"{scene.Settings.SamplesPerPixel} spp"
                : $"PPM — {scene.Integrator.PhotonsPerPass:N0} photons/pass";

            IsPhotonMapping = scene.Integrator.Type == IntegratorType.PhotonMapping;

            _tileSize = 16;
            _samplesPerPixel = scene.Settings.SamplesPerPixel;
            _renderStartTime = DateTime.UtcNow;

            CanSaveImage = false;
            SaveImageCommand.NotifyCanExecuteChanged();

            CanApplyDebugView = false;
            ApplyDebugViewCommand.NotifyCanExecuteChanged();

            if (scene.Integrator.Type == IntegratorType.PhotonMapping)
            {
                // ── Photon mapping renderer ───────────────────────────────────────
                _photonRenderer = new PhotonMappingRenderer();
                var tilesX = (int)Math.Ceiling(
                    (double)scene.Settings.ImageWidth / _photonRenderer.TileSize);
                var tilesY = (int)Math.Ceiling(
                    (double)scene.Settings.ImageHeight / _photonRenderer.TileSize);
                _totalTiles = tilesX * tilesY;

                _frameBuffer = await _photonRenderer.RenderAsync(
                    scene,
                    onProgress: progress =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                            UpdatePhotonProgress(progress));
                    },
                    cancellationToken: _cts.Token);
            }
            else
            {
                // ── Progressive path tracing renderer ────────────────────────────
                var renderer = new ProgressiveRenderer();
                var tilesX = (int)Math.Ceiling(
                    (double)scene.Settings.ImageWidth / renderer.TileSize);
                var tilesY = (int)Math.Ceiling(
                    (double)scene.Settings.ImageHeight / renderer.TileSize);
                _totalTiles = tilesX * tilesY;
                _tileSize = renderer.TileSize;

                _frameBuffer = await renderer.RenderAsync(
                    scene,
                    onProgress: progress =>
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                            UpdateProgress(progress));
                    },
                    cancellationToken: _cts.Token);
            }

            // Always display the final frame
            Application.Current.Dispatcher.Invoke(() => UpdateBitmap(_frameBuffer));

            StatusText = _cts.Token.IsCancellationRequested
                ? "Render aborted"
                : IsPhotonMapping
                    ? $"Done — {CurrentPassText} passes"
                    : $"Done — {scene.Settings.SamplesPerPixel} spp";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Render aborted";
        }
        catch (Exception ex)
        {
            ValidationMessages.Add(new ValidationMessage(
                ValidationSeverity.Error, $"Unexpected error: {ex.Message}"));
            StatusText = "Error — see messages below";
        }
        finally
        {
            IsRendering = false;
            
            _cts?.Dispose();
            _cts = null;

            // After render completes, in the finally block
            CanSaveImage = !(_cts?.IsCancellationRequested ?? false);
            SaveImageCommand.NotifyCanExecuteChanged();

            // Enable debug view only for completed photon mapping renders
            CanApplyDebugView = IsPhotonMapping &&
                                !(_cts?.IsCancellationRequested ?? false) &&
                                _photonRenderer?.LastPhotonMap is not null;
            ApplyDebugViewCommand.NotifyCanExecuteChanged();

            RunCommand.NotifyCanExecuteChanged();
            AbortCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanRun() => !IsRendering && !IsWarmingUp;

    [RelayCommand(CanExecute = nameof(CanAbort))]
    private void Abort()
    {
        _cts?.Cancel();
        StatusText = "Aborting...";
    }

    private bool CanAbort() => IsRendering;

    [RelayCommand]
    private void NewScript()
    {
        if (!ConfirmDiscardChanges()) return;
        ScriptText = DefaultScript;
        _currentFilePath = null;
        UpdateWindowTitle();
        ValidationMessages.Clear();
        StatusText = "Ready";
    }

    [RelayCommand]
    private void OpenScript()
    {
        if (!ConfirmDiscardChanges()) return;

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Open Scene Script",
            Filter = "C# Scene Scripts (*.cs)|*.cs|All Files (*.*)|*.*",
            DefaultExt = ".cs",
            InitialDirectory = AppContext.BaseDirectory
        };

        if (dialog.ShowDialog() != true) return;

        LoadScriptFromPath(dialog.FileName);
    }

    [RelayCommand]
    private void SaveScript()
    {
        if (_currentFilePath is null)
            SaveScriptAs();
        else
            WriteFile(_currentFilePath);
    }

    [RelayCommand]
    private void SaveScriptAs()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Save Scene Script",
            Filter = "C# Scene Scripts (*.cs)|*.cs|All Files (*.*)|*.*",
            DefaultExt = ".cs",
            InitialDirectory = AppContext.BaseDirectory,
            FileName = _currentFilePath is null
                ? "scene"
                : Path.GetFileName(_currentFilePath)
        };

        if (dialog.ShowDialog() != true) return;

        _currentFilePath = dialog.FileName;
        WriteFile(_currentFilePath);
        UpdateWindowTitle();
    }

    [RelayCommand]
    private void OpenRecentFile(string path)
    {
        if (!ConfirmDiscardChanges()) return;
        LoadScriptFromPath(path);
    }

    [RelayCommand]
    private void ClearRecentFiles()
    {
        _recentFilesService.ClearRecentFiles();
        LoadRecentFiles();
    }

    [RelayCommand(CanExecute = nameof(CanSaveImage))]
    private void SaveImage()
    {
        if (PreviewBitmap is null) return;

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Save Rendered Image",
            Filter = "PNG Image (*.png)|*.png",
            DefaultExt = ".png",
            FileName = _currentFilePath is null
                ? "render"
                : Path.GetFileNameWithoutExtension(_currentFilePath)
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            using var stream = File.Create(dialog.FileName);
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(PreviewBitmap));
            encoder.Save(stream);
            StatusText = $"Image saved — {Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to save image:\n{ex.Message}",
                "Save Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    [RelayCommand(CanExecute = nameof(CanApplyDebugView))]
    private async Task ApplyDebugViewAsync()
    {
        StatusText = $"Applying debug view: {SelectedDebugMode}...";

        if (SelectedDebugMode == PhotonDebugMode.None)
        {
            UpdateBitmap(_frameBuffer!);
            return;
        }

        if (_photonRenderer?.LastPhotonMap is null ||
            _photonRenderer?.LastPixelStates is null)
            return;

        var context = _photonRenderer.GetDebugContext();
        if (context is null) return;

        var debugFb = await Task.Run(() =>
            _debugRenderer.Render(
                SelectedDebugMode,
                _photonRenderer.LastPhotonMap,
                _photonRenderer.LastPixelStates,
                context));

        UpdateBitmap(debugFb);
        StatusText = $"Debug view: {SelectedDebugMode}";
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void LoadScriptFromPath(string path)
    {
        try
        {
            ScriptText = File.ReadAllText(path);
            _currentFilePath = path;
            UpdateWindowTitle();
            ValidationMessages.Clear();
            StatusText = "Script loaded";
            _recentFilesService.AddRecentFile(path);
            LoadRecentFiles();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to open file:\n{ex.Message}",
                "Open Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            _recentFilesService.RemoveRecentFile(path);
            LoadRecentFiles();
        }
    }

    private void UpdateProgress(RenderProgress progress)
    {
        ProgressPercent = progress.PercentComplete;

        var elapsed = progress.Elapsed;
        var elapsedSeconds = elapsed.TotalSeconds;

        // Rays traced = tiles × pixels per tile × spp
        var pixelsPerTile = _tileSize * _tileSize;
        var raysTraced = (long)progress.TilesCompleted
                         * pixelsPerTile
                         * _samplesPerPixel;

        var raysPerSec = elapsedSeconds > 0
            ? raysTraced / elapsedSeconds
            : 0;

        var tilesPerSec = elapsedSeconds > 0
            ? progress.TilesCompleted / elapsedSeconds
            : 0;

        // Update statistics
        ElapsedText = elapsed.ToString(@"mm\:ss");
        TilesText = $"{progress.TilesCompleted} / {progress.TotalTiles}";
        TilesPerSecText = $"{tilesPerSec:F1}";
        RaysPerSecText = FormatRaysPerSec(raysPerSec);
        ProgressText = $"{progress.PercentComplete:F0}%  {elapsed:mm\\:ss}";

        // Throttle bitmap updates to ~10fps
        var now = DateTime.UtcNow;
        if ((now - _lastBitmapUpdate).TotalMilliseconds >= 100)
        {
            UpdateBitmap(progress.FrameBuffer);
            _lastBitmapUpdate = now;
        }
    }

    private void UpdatePhotonProgress(PhotonMappingProgress progress)
    {
        CurrentPassText = progress.Pass.ToString();
        TotalPhotonsText = FormatRaysPerSec(progress.TotalPhotons);
        AverageRadiusText = $"{progress.AverageRadius:F4}";

        var elapsed = progress.Elapsed;
        ElapsedText = elapsed.ToString(@"mm\:ss");
        ProgressText = $"Pass {progress.Pass}" +
                       $"  {progress.TotalPhotons:N0} photons" +
                       $"  r={progress.AverageRadius:F4}" +
                       $"  {elapsed:mm\\:ss}";

        var now = DateTime.UtcNow;
        if ((now - _lastBitmapUpdate).TotalMilliseconds >= 100)
        {
            UpdateBitmap(progress.CombinedFrameBuffer);
            _lastBitmapUpdate = now;
        }
    }

    private static string FormatRaysPerSec(double raysPerSec)
    {
        if (raysPerSec >= 1_000_000)
            return $"{raysPerSec / 1_000_000:F1}M";
        if (raysPerSec >= 1_000)
            return $"{raysPerSec / 1_000:F1}K";
        return $"{raysPerSec:F0}";
    }

    private void UpdateBitmap(FrameBuffer frameBuffer)
    {
        if (PreviewBitmap is null) return;

        var width = frameBuffer.Width;
        var height = frameBuffer.Height;
        var stride = width * 3; // Rgb24 = 3 bytes per pixel
        var pixels = new byte[stride * height];

        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var (r, g, b) = frameBuffer.GetDisplayPixel(x, y);
                var idx = y * stride + x * 3;
                pixels[idx] = (byte)Math.Clamp(r * 255.999, 0, 255);
                pixels[idx + 1] = (byte)Math.Clamp(g * 255.999, 0, 255);
                pixels[idx + 2] = (byte)Math.Clamp(b * 255.999, 0, 255);
            }

        PreviewBitmap.WritePixels(
            new Int32Rect(0, 0, width, height),
            pixels, stride, 0);
    }

    private void WriteFile(string path)
    {
        try
        {
            File.WriteAllText(path, ScriptText);
            StatusText = $"Saved — {Path.GetFileName(path)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to save file:\n{ex.Message}",
                "Save Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private bool ConfirmDiscardChanges()
    {
        // For now always allow — could track dirty state later
        return true;
    }

    private void UpdateWindowTitle()
    {
        WindowTitle = _currentFilePath is null
            ? "PathTracer"
            : $"PathTracer — {Path.GetFileName(_currentFilePath)}";
    }

    private void LoadRecentFiles()
{
    RecentFiles.Clear();
    foreach (var file in _recentFilesService.GetRecentFiles())
        RecentFiles.Add(file);
}

    // ── The Application will signal this ─────────────────────────────────────

    public void OnWarmupComplete()
    {
        IsWarmingUp = false;
        WarmupStatusText = "Ready";
        RunCommand.NotifyCanExecuteChanged();
    }

    // ── Default script ────────────────────────────────────────────────────────

    private const string DefaultScript = """
        // Cornell Box — edit and press Run to render
        return Scene
            .WithCamera(
                position: new Vector3(0, 0, 3.5),
                lookAt: Vector3.Zero,
                fovDegrees: 40)
            .WithRenderSettings(
                imageWidth: 512,
                imageHeight: 512,
                samplesPerPixel: 64)
            .AddQuad(new Vector3(-1, -1, -1), new Vector3(2, 0, 0),
                new Vector3(0, 0, 2),
                MaterialBuilder.Lambertian(new Vector3(0.73, 0.73, 0.73)),
                name: "Floor")
            .AddQuad(new Vector3(-1, 1, -1), new Vector3(2, 0, 0),
                new Vector3(0, 0, 2),
                MaterialBuilder.Lambertian(new Vector3(0.73, 0.73, 0.73)),
                name: "Ceiling")
            .AddQuad(new Vector3(-1, -1, -1), new Vector3(2, 0, 0),
                new Vector3(0, 2, 0),
                MaterialBuilder.Lambertian(new Vector3(0.73, 0.73, 0.73)),
                name: "BackWall")
            .AddQuad(new Vector3(-1, -1, -1), new Vector3(0, 2, 0),
                new Vector3(0, 0, 2),
                MaterialBuilder.Lambertian(new Vector3(0.65, 0.05, 0.05)),
                name: "LeftWall")
            .AddQuad(new Vector3(1, -1, -1), new Vector3(0, 2, 0),
                new Vector3(0, 0, 2),
                MaterialBuilder.Lambertian(new Vector3(0.12, 0.45, 0.15)),
                name: "RightWall")
            .AddSphere(
                centre: new Vector3(0.35, -0.55, 0.2),
                radius: 0.45,
                material: MaterialBuilder.Dielectric(ior: 1.5),
                name: "GlassBall")
            .AddSphere(
                centre: new Vector3(-0.35, -0.55, -0.2),
                radius: 0.45,
                material: MaterialBuilder.GgxMetal(
                    new Vector3(0.95, 0.93, 0.88), roughness: 0.05),
                name: "SilverBall")
            .AddAreaLight(
                corner: new Vector3(-0.25, 0.999, -0.25),
                edge1: new Vector3(0.5, 0, 0),
                edge2: new Vector3(0, 0, 0.5),
                emission: new Vector3(15, 15, 15),
                name: "CeilingLight")
            .Build();
        """;
}