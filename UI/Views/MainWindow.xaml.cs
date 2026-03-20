using System.Windows;
using UI.ViewModels;

namespace UI.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        // AvalonEdit does not support MVVM binding for its Text property
        // directly, so we wire it up manually in code-behind
        ScriptEditor.Text = _viewModel.ScriptText;

        ScriptEditor.TextChanged += (_, _) =>
            _viewModel.ScriptText = ScriptEditor.Text;

        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.ScriptText) &&
                ScriptEditor.Text != _viewModel.ScriptText)
                ScriptEditor.Text = _viewModel.ScriptText;
        };
    }
}
