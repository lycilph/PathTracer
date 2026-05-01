using System.Windows;

namespace Studio;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var vm = new MainViewModel();
        DataContext = vm;

        vm.GoToRequested += (line, col) => ScriptEditor.GoTo(line, col);
    }
}