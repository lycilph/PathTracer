using System.Windows;
using UI.ViewModels;

namespace UI.Views;

public partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; init; }

    public MainWindow()
    {
        InitializeComponent();
        ViewModel = new MainViewModel();
        DataContext = ViewModel;

        // AvalonEdit does not support MVVM binding for its Text property
        // directly, so we wire it up manually in code-behind
        ScriptEditor.Text = ViewModel.ScriptText;

        ScriptEditor.TextChanged += (_, _) =>
            ViewModel.ScriptText = ScriptEditor.Text;

        ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.ScriptText) &&
                ScriptEditor.Text != ViewModel.ScriptText)
                ScriptEditor.Text = ViewModel.ScriptText;
        };
    }
}
