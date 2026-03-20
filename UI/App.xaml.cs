using System.Windows;
using UI.ViewModels;
using UI.Views;

namespace UI;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        var mainWindow = new MainWindow();
        MainWindow = mainWindow;
        mainWindow.Show();

        _ = WarmUpRoslynAsync(mainWindow.ViewModel);
    }

    private static async Task WarmUpRoslynAsync(MainViewModel viewModel)
    {
        await Task.Run(async () =>
        {
            var compiler = new Scripting.ScriptCompiler();
            await compiler.CompileAndRunAsync("""
            return Scene
                .WithCamera(
                    position: new Vector3(0, 0, 1),
                    lookAt: Vector3.Zero,
                    fovDegrees: 40)
                .WithRenderSettings(1, 1, 1)
                .AddAreaLight(
                    corner: new Vector3(-0.5, 0.9, -0.5),
                    edge1: new Vector3(1, 0, 0),
                    edge2: new Vector3(0, 0, 1),
                    emission: new Vector3(1, 1, 1))
                .Build();
            """);
        });

        Current.Dispatcher.Invoke(viewModel.OnWarmupComplete);
    }
}