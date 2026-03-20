using System.Windows.Input;

namespace UI.Misc;

/// <summary>
/// Thin adapter to use a CommunityToolkit RelayCommand as a WPF ICommand
/// in KeyBinding.
/// </summary>
internal sealed class RelayCommandAdapter : ICommand
{
    private readonly CommunityToolkit.Mvvm.Input.IRelayCommand _inner;

    public RelayCommandAdapter(CommunityToolkit.Mvvm.Input.IRelayCommand inner)
        => _inner = inner;

    public bool CanExecute(object? parameter) => _inner.CanExecute(parameter);
    public void Execute(object? parameter) => _inner.Execute(parameter);
    public event EventHandler? CanExecuteChanged
    {
        add => _inner.CanExecuteChanged += value;
        remove => _inner.CanExecuteChanged -= value;
    }
}