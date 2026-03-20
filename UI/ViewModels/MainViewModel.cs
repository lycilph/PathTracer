using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Engine.Rendering;
using ScriptApi.Validation;
using UI.Rendering;
using UI.Scripting;

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

    // ── Script editor ─────────────────────────────────────────────────────────

    [ObservableProperty]
    private string _scriptText = DefaultScript;

    // ── Status ────────────────────────────────────────────────────────────────

    [ObservableProperty]
    private string _statusText = "Ready";

    [ObservableProperty]
    private double _progressPercent;

    [ObservableProperty]
    private string _progressText = string.Empty;

    [ObservableProperty]
    private bool _isRendering;

    // ── Preview image ─────────────────────────────────────────────────────────

    [ObservableProperty]
    private WriteableBitmap? _previewBitmap;

    // ── Validation messages ───────────────────────────────────────────────────

    public ObservableCollection<ValidationMessage> ValidationMessages { get; } = [];

    // ── Window title ──────────────────────────────────────────────────────────

    [ObservableProperty]
    private string _windowTitle = "PathTracer";

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
                foreach (var error in scriptResult.Errors)
                    ValidationMessages.Add(new ValidationMessage(
                        ValidationSeverity.Error, error));

                StatusText = $"Compilation failed — " +
                             $"{scriptResult.Errors.Count} error(s)";
                return;
            }

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
            var renderer = new ProgressiveRenderer();

            var frameBuffer = await renderer.RenderAsync(
                scene,
                onProgress: progress =>
                {
                    Application.Current.Dispatcher.Invoke(() =>
                        UpdateProgress(progress));
                },
                cancellationToken: _cts.Token);

            // Always display the final frame
            Application.Current.Dispatcher.Invoke(() => UpdateBitmap(frameBuffer));

            StatusText = _cts.Token.IsCancellationRequested
                ? "Render aborted"
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
            RunCommand.NotifyCanExecuteChanged();
            AbortCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanRun() => !IsRendering;

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
            DefaultExt = ".cs"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            ScriptText = File.ReadAllText(dialog.FileName);
            _currentFilePath = dialog.FileName;
            UpdateWindowTitle();
            ValidationMessages.Clear();
            StatusText = "Script loaded";
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to open file:\n{ex.Message}",
                "Open Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
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
            FileName = _currentFilePath is null
                ? "scene"
                : Path.GetFileName(_currentFilePath)
        };

        if (dialog.ShowDialog() != true) return;

        _currentFilePath = dialog.FileName;
        WriteFile(_currentFilePath);
        UpdateWindowTitle();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private void UpdateProgress(RenderProgress progress)
    {
        ProgressPercent = progress.PercentComplete;
        ProgressText = $"{progress.TilesCompleted}/{progress.TotalTiles} tiles" +
                       $"  {progress.PercentComplete:F0}%" +
                       $"  {progress.Elapsed:mm\\:ss}";

        // Only update bitmap every 100ms
        var now = DateTime.UtcNow;
        if ((now - _lastBitmapUpdate).TotalMilliseconds >= 100)
        {
            UpdateBitmap(progress.FrameBuffer);
            _lastBitmapUpdate = now;
        }
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