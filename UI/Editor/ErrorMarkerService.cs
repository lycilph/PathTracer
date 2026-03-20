using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Rendering;
using UI.Scripting;

namespace UI.Editor;

/// <summary>
/// Draws red squiggly underlines on lines that contain compilation errors
/// and shows the error message in a tooltip on hover.
/// </summary>
public sealed class ErrorMarkerService : IBackgroundRenderer
{
    private readonly TextEditor _editor;
    private readonly List<ErrorMarker> _markers = [];
    private ToolTip? _toolTip;

    public ErrorMarkerService(TextEditor editor)
    {
        _editor = editor;

        // Hook mouse events for tooltip
        _editor.TextArea.TextView.MouseMove += OnMouseMove;
        _editor.TextArea.TextView.MouseLeave += OnMouseLeave;
    }

    /// <summary>
    /// Updates the error markers from the latest compilation result.
    /// </summary>
    public void SetErrors(IReadOnlyList<ScriptError> errors)
    {
        _markers.Clear();

        foreach (var error in errors.Where(e => e.HasLocation))
        {
            // Guard against line numbers outside the document
            if (error.Line < 1 || error.Line > _editor.Document.LineCount)
                continue;

            var line = _editor.Document.GetLineByNumber(error.Line);
            _markers.Add(new ErrorMarker(
                line.Offset,
                line.Length,
                error.Message));
        }

        // Trigger a redraw
        _editor.TextArea.TextView.InvalidateLayer(KnownLayer.Selection);
    }

    /// <summary>Clears all error markers.</summary>
    public void ClearErrors()
    {
        _markers.Clear();
        _editor.TextArea.TextView.InvalidateLayer(KnownLayer.Selection);
    }

    // ── IBackgroundRenderer ───────────────────────────────────────────────────

    public KnownLayer Layer => KnownLayer.Selection;

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (_markers.Count == 0) return;

        textView.EnsureVisualLines();

        foreach (var marker in _markers)
        {
            // Only draw markers that are within the visible range
            var startOffset = Math.Max(marker.StartOffset,
                textView.VisualLines.First().FirstDocumentLine.Offset);
            var endOffset = Math.Min(marker.StartOffset + marker.Length,
                textView.VisualLines.Last().LastDocumentLine.EndOffset);

            if (startOffset >= endOffset) continue;

            var segment = new ICSharpCode.AvalonEdit.Document.TextSegment
            {
                StartOffset = startOffset,
                Length = endOffset - startOffset
            };

            foreach (var rect in BackgroundGeometryBuilder
                         .GetRectsForSegment(textView, segment))
            {
                DrawSquiggle(drawingContext, rect);
            }
        }
    }

    // ── Squiggle drawing ──────────────────────────────────────────────────────

    private static void DrawSquiggle(DrawingContext dc, Rect rect)
    {
        // Draw a squiggly line along the bottom of the text rect
        var pen = new Pen(Brushes.Red, 1.0);
        pen.Freeze();

        var y = rect.Bottom - 1;
        var geometry = new StreamGeometry();

        using (var ctx = geometry.Open())
        {
            ctx.BeginFigure(new Point(rect.Left, y), false, false);

            // Zigzag with 3px wavelength, 2px amplitude
            var x = rect.Left;
            var up = true;
            while (x < rect.Right)
            {
                x += 3;
                ctx.LineTo(
                    new Point(Math.Min(x, rect.Right), up ? y - 2 : y),
                    true, false);
                up = !up;
            }
        }

        geometry.Freeze();
        dc.DrawGeometry(null, pen, geometry);
    }

    // ── Tooltip ───────────────────────────────────────────────────────────────

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        var pos = e.GetPosition(_editor.TextArea.TextView);
        var offset = GetOffsetFromPosition(pos);
        if (offset < 0)
        {
            HideToolTip();
            return;
        }

        var marker = _markers.FirstOrDefault(
            m => offset >= m.StartOffset &&
                 offset <= m.StartOffset + m.Length);

        if (marker is null)
        {
            HideToolTip();
            return;
        }

        if (_toolTip is null)
        {
            _toolTip = new ToolTip
            {
                Placement = System.Windows.Controls
                                  .Primitives.PlacementMode.Mouse,
                PlacementTarget = _editor.TextArea.TextView,
                StaysOpen = false
            };
        }

        _toolTip.Content = new TextBlock
        {
            Text = marker.Message,
            MaxWidth = 400,
            TextWrapping = TextWrapping.Wrap,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 12
        };

        _toolTip.IsOpen = true;
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
        => HideToolTip();

    private void HideToolTip()
    {
        if (_toolTip is not null)
            _toolTip.IsOpen = false;
    }

    private int GetOffsetFromPosition(Point position)
    {
        try
        {
            var textView = _editor.TextArea.TextView;

            // Convert mouse position to document position
            var docPos = textView.GetPosition(
                position + new Vector(textView.ScrollOffset.X,
                                      textView.ScrollOffset.Y));

            if (docPos is null) return -1;

            return _editor.Document.GetOffset(
                docPos.Value.Line,
                docPos.Value.Column);
        }
        catch
        {
            return -1;
        }
    }
}

/// <summary>
/// Represents a single error marker in the editor.
/// </summary>
internal sealed class ErrorMarker
{
    public int StartOffset { get; }
    public int Length { get; }
    public string Message { get; }

    public ErrorMarker(int startOffset, int length, string message)
    {
        StartOffset = startOffset;
        Length = length;
        Message = message;
    }
}