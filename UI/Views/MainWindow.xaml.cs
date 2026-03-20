using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using UI.Editor;
using UI.Misc;
using UI.ViewModels;

namespace UI.Views;

public partial class MainWindow : Window
{
    private readonly ErrorMarkerService _errorMarkerService;

    public MainViewModel ViewModel { get; init; }

    public MainWindow()
    {
        InitializeComponent();
        ViewModel = new MainViewModel();
        DataContext = ViewModel;

        // AvalonEdit text binding
        ScriptEditor.Text = ViewModel.ScriptText;

        ScriptEditor.TextChanged += (_, _) =>
            ViewModel.ScriptText = ScriptEditor.Text;

        ViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.ScriptText) &&
                ScriptEditor.Text != ViewModel.ScriptText)
                ScriptEditor.Text = ViewModel.ScriptText;

            // Update error markers when compilation errors change
            if (e.PropertyName == nameof(MainViewModel.LastScriptErrors) && _errorMarkerService != null)
            {
                var errors = ViewModel.LastScriptErrors;
                if (errors.Count == 0 )
                    _errorMarkerService.ClearErrors();
                else
                    _errorMarkerService.SetErrors(errors);
            }
        };

        // Register error marker service with AvalonEdit
        _errorMarkerService = new ErrorMarkerService(ScriptEditor);
        ScriptEditor.TextArea.TextView.BackgroundRenderers
                    .Add(_errorMarkerService);

        // Keyboard shortcuts
        CommandBindings.Add(new CommandBinding(
            ApplicationCommands.New,
            (_, _) => ViewModel.NewScriptCommand.Execute(null)));

        CommandBindings.Add(new CommandBinding(
            ApplicationCommands.Open,
            (_, _) => ViewModel.OpenScriptCommand.Execute(null)));

        CommandBindings.Add(new CommandBinding(
            ApplicationCommands.Save,
            (_, _) => ViewModel.SaveScriptCommand.Execute(null)));

        CommandBindings.Add(new CommandBinding(
            ApplicationCommands.SaveAs,
            (_, _) => ViewModel.SaveScriptAsCommand.Execute(null)));

        // Ctrl+I for save image
        var saveImageGesture = new KeyBinding(
            new RelayCommandAdapter(ViewModel.SaveImageCommand),
            new KeyGesture(Key.I, ModifierKeys.Control));
        InputBindings.Add(saveImageGesture);

        // F5 for run
        var runGesture = new KeyBinding(
            new RelayCommandAdapter(ViewModel.RunCommand),
            new KeyGesture(Key.F5));
        InputBindings.Add(runGesture);

        // Escape for abort
        var abortGesture = new KeyBinding(
            new RelayCommandAdapter(ViewModel.AbortCommand),
            new KeyGesture(Key.Escape));
        InputBindings.Add(abortGesture);
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
       => Application.Current.Shutdown();
    
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        ConstrainMenuPopups(MainMenu);
    }

    private static void ConstrainMenuPopups(ItemsControl menu)
    {
        foreach (var item in menu.Items.OfType<MenuItem>())
        {
            item.SubmenuOpened += (sender, _) =>
            {
                if (sender is not MenuItem menuItem) return;

                // Find the popup in the visual tree
                var popup = menuItem.Template
                    .FindName("PART_Popup", menuItem) as System.Windows.Controls.Primitives.Popup;

                if (popup is null) return;

                popup.Placement = System.Windows.Controls.Primitives.PlacementMode.Custom;
                popup.PlacementTarget = menuItem;
                popup.HorizontalOffset = 0;
                popup.VerticalOffset = 0;

                popup.CustomPopupPlacementCallback = (popupSize, targetSize, offset) =>
                {
                    // Place popup below the menu item, left edges aligned
                    return
                    [
                        new System.Windows.Controls.Primitives
                        .CustomPopupPlacement(
                            new Point(0, targetSize.Height),
                            System.Windows.Controls.Primitives
                                .PopupPrimaryAxis.Horizontal)
                    ];
                };
            };
        }
    }
}
