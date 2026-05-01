using System.Windows;
using System.Windows.Controls;
using ICSharpCode.AvalonEdit.Highlighting;

namespace Studio;

public partial class SceneScriptEditor : UserControl
{
    public SceneScriptEditor()
    {
        InitializeComponent();

        // Enable built-in C# highlighting
        // AvalonEdit provides built-in highlighting definitions (including C#). [4](https://github.com/icsharpcode/AvalonEdit/blob/master/ICSharpCode.AvalonEdit/Highlighting/Resources/CSharp-Mode.xshd)[3](http://avalonedit.net/documentation/html/4d4ceb51-154d-43f0-b876-ad9640c5d2d8.htm)
        Editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");

        Editor.TextChanged += (_, _) =>
        {
            if (!_suppressTextChange)
                Text = Editor.Text;
        };
    }

    // Two-way bindable Text
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(SceneScriptEditor),
            new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var c = (SceneScriptEditor)d;
        c.SetEditorText((string?)e.NewValue ?? "");
    }

    private bool _suppressTextChange;

    private void SetEditorText(string text)
    {
        if (Editor.Text == text)
            return;

        _suppressTextChange = true;
        try { Editor.Text = text; }
        finally { _suppressTextChange = false; }
    }

    // Diagnostics + navigation
    public static readonly DependencyProperty DiagnosticsProperty =
        DependencyProperty.Register(
            nameof(Diagnostics),
            typeof(IEnumerable<ScriptDiagnosticItem>),
            typeof(SceneScriptEditor),
            new PropertyMetadata(Array.Empty<ScriptDiagnosticItem>()));

    public IEnumerable<ScriptDiagnosticItem> Diagnostics
    {
        get => (IEnumerable<ScriptDiagnosticItem>)GetValue(DiagnosticsProperty);
        set => SetValue(DiagnosticsProperty, value);
    }

    public static readonly DependencyProperty SelectedDiagnosticProperty =
        DependencyProperty.Register(
            nameof(SelectedDiagnostic),
            typeof(ScriptDiagnosticItem),
            typeof(SceneScriptEditor),
            new PropertyMetadata(null));

    public ScriptDiagnosticItem? SelectedDiagnostic
    {
        get => (ScriptDiagnosticItem?)GetValue(SelectedDiagnosticProperty);
        set => SetValue(SelectedDiagnosticProperty, value);
    }

    public static readonly DependencyProperty GoToDiagnosticCommandProperty =
        DependencyProperty.Register(
            nameof(GoToDiagnosticCommand),
            typeof(System.Windows.Input.ICommand),
            typeof(SceneScriptEditor),
            new PropertyMetadata(null));

    public System.Windows.Input.ICommand? GoToDiagnosticCommand
    {
        get => (System.Windows.Input.ICommand?)GetValue(GoToDiagnosticCommandProperty);
        set => SetValue(GoToDiagnosticCommandProperty, value);
    }

    public void GoTo(int line, int column)
    {
        // AvalonEdit uses 1-based line and 1-based column
        line = Math.Max(1, line);
        column = Math.Max(1, column);

        Editor.ScrollTo(line, column);
        Editor.TextArea.Caret.Location = new ICSharpCode.AvalonEdit.Document.TextLocation(line, column);
        Editor.TextArea.Focus();
    }
}
