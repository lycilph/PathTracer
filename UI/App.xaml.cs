using System.Windows;

namespace UI;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _ = WarmUpRoslynAsync();
    }

    private static async Task WarmUpRoslynAsync()
    {
        await Task.Run(() =>
            Microsoft.CodeAnalysis.CSharp.Scripting.CSharpScript
                .RunAsync("1+1"));
    }
}