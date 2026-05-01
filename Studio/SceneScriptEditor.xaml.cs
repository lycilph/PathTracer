using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using Studio.Editor;

namespace Studio;

public partial class SceneScriptEditor : UserControl
{
    private TextMarkerService? _markerService;
    private INotifyCollectionChanged? _currentDiagCollection;

    public SceneScriptEditor()
    {
        InitializeComponent();

        // Enable built-in C# highlighting
        // AvalonEdit provides built-in highlighting definitions (including C#). [4](https://github.com/icsharpcode/AvalonEdit/blob/master/ICSharpCode.AvalonEdit/Highlighting/Resources/CSharp-Mode.xshd)[3](http://avalonedit.net/documentation/html/4d4ceb51-154d-43f0-b876-ad9640c5d2d8.htm)
        Editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("C#");

        // Install marker service for squiggles
        _markerService = new TextMarkerService(Editor.Document);
        Editor.TextArea.TextView.BackgroundRenderers.Add(_markerService);

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
            new PropertyMetadata(Array.Empty<ScriptDiagnosticItem>(), OnDiagnosticsChanged));

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
        Editor.TextArea.Caret.Location = new TextLocation(line, column);
        Editor.TextArea.Focus();
    }

    private static void OnDiagnosticsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var c = (SceneScriptEditor)d;
        c.DetachDiagnosticsCollection(e.OldValue);
        c.AttachDiagnosticsCollection(e.NewValue);
        c.UpdateSquiggles();
    }

    private void AttachDiagnosticsCollection(object? newValue)
    {
        if (newValue is INotifyCollectionChanged incc)
        {
            _currentDiagCollection = incc;
            incc.CollectionChanged += Diagnostics_CollectionChanged;
        }
    }

    private void DetachDiagnosticsCollection(object? oldValue)
    {
        if (_currentDiagCollection is not null)
        {
            _currentDiagCollection.CollectionChanged -= Diagnostics_CollectionChanged;
            _currentDiagCollection = null;
        }
    }

    private void Diagnostics_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Runs on UI thread (your VM updates collection via _ui.Post), so safe to update editor here.
        UpdateSquiggles();
    }

    private void UpdateSquiggles()
    {
        if (_markerService is null || Editor.Document is null)
            return;

        _markerService.Clear();

        var diags = Diagnostics ?? Array.Empty<ScriptDiagnosticItem>();

        foreach (var diag in diags)
        {
            // Only draw squiggles for errors (you can include warnings too if you want)
            if (!(string.Equals(diag.Severity, "Error", StringComparison.OrdinalIgnoreCase) ||
                  string.Equals(diag.Severity, "Warning", StringComparison.OrdinalIgnoreCase)))
                continue;

            var color = diag.Severity.Equals("Warning", StringComparison.OrdinalIgnoreCase) ? Colors.Goldenrod : Colors.Red;

            // Map (line,col) -> document offset
            // AvalonEdit uses offsets as indices into TextDocument; GetOffset maps (line,column) to offset. [5](https://danielgrunwald.de/coding/AvalonEdit/document.php)[4](http://avalonedit.net/documentation/html/756feb0b-9e70-0fd8-eb8e-686484941410.htm)
            int line = Math.Max(1, diag.Line);
            int col = Math.Max(1, diag.Column);

            int offset;
            try
            {
                offset = Editor.Document.GetOffset(line, col);
            }
            catch
            {
                // Out of range line/column; skip
                continue;
            }

            int length = GuessTokenLength(Editor.Document, offset);
            if (length <= 0) length = 1;

            _markerService.Create(
                startOffset: offset,
                length: length,
                color: color,
                toolTip: diag.Display);
        }

        // Force redraw because squiggles are external data to the renderer. [1](http://avalonedit.net/documentation/html/c06e9832-9ef0-4d65-ac2e-11f7ce9c7774.htm)[6](https://www.danielgrunwald.de/coding/AvalonEdit/rendering.php)
        Editor.TextArea.TextView.Redraw();
    }

    private static int GuessTokenLength(TextDocument doc, int offset)
    {
        // Simple heuristic: underline until whitespace or common delimiter.
        // This avoids squiggling just a single character most of the time.
        const string stops = " \t\r\n;,.(){}[]<>:+-*/=!&|^%\"'";

        int max = doc.TextLength;
        if (offset < 0 || offset >= max) return 0;

        int i = offset;
        while (i < max)
        {
            char c = doc.GetCharAt(i);
            if (stops.IndexOf(c) >= 0)
                break;
            i++;
        }

        return Math.Max(1, i - offset);
    }
}
