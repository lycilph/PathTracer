using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace Studio.Editor;

/// <summary>
/// Minimal text marker service inspired by the SharpDevelop/AvalonEdit TextMarkerService pattern:
/// - markers stored in TextSegmentCollection so they move with document edits
/// - draws a wavy underline ("squiggle") for visible markers
/// </summary>
/// <remarks>
/// This uses AvalonEdit rendering extension points (background renderer) which are designed
/// for non-interactive visuals like markers/squiggles. [1](http://avalonedit.net/documentation/html/c06e9832-9ef0-4d65-ac2e-11f7ce9c7774.htm)[2](http://avalonedit.net/documentation/html/fe379977-4956-6a16-4a74-1d574b684ef4.htm)
/// </remarks>
public sealed class TextMarkerService : IBackgroundRenderer, IDisposable
{
    private readonly TextDocument _document;
    private readonly TextSegmentCollection<TextMarker> _markers;

    public TextMarkerService(TextDocument document)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _markers = new TextSegmentCollection<TextMarker>(_document);
    }

    public KnownLayer Layer => KnownLayer.Selection; // draw above background but under selection/caret

    public void Dispose()
    {
        _markers.Clear();
    }

    public void Clear()
    {
        _markers.Clear();
    }

    public TextMarker Create(int startOffset, int length, Color color, string? toolTip = null)
    {
        startOffset = Math.Max(0, Math.Min(startOffset, _document.TextLength));
        length = Math.Max(0, Math.Min(length, _document.TextLength - startOffset));

        var marker = new TextMarker
        {
            StartOffset = startOffset,
            Length = length,
            Color = color,
            ToolTip = toolTip
        };

        _markers.Add(marker);
        return marker;
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (textView is null) throw new ArgumentNullException(nameof(textView));
        if (drawingContext is null) throw new ArgumentNullException(nameof(drawingContext));

        if (!textView.VisualLinesValid)
            return;

        if (_markers.Count == 0)
            return;

        // Draw only markers that intersect currently visible lines
        foreach (var marker in GetVisibleMarkers(textView))
        {
            DrawWavyUnderline(textView, drawingContext, marker);
        }
    }

    private IEnumerable<TextMarker> GetVisibleMarkers(TextView textView)
    {
        // Visual lines cover the visible region only; this is why background renderers are fast. [1](http://avalonedit.net/documentation/html/c06e9832-9ef0-4d65-ac2e-11f7ce9c7774.htm)[6](https://www.danielgrunwald.de/coding/AvalonEdit/rendering.php)
        if (textView.VisualLines.Count == 0)
            yield break;

        int viewStart = textView.VisualLines.First().FirstDocumentLine.Offset;
        int viewEnd = textView.VisualLines.Last().LastDocumentLine.EndOffset;

        // TextSegmentCollection supports efficient offset queries when attached to a document. [4](http://avalonedit.net/documentation/html/756feb0b-9e70-0fd8-eb8e-686484941410.htm)
        foreach (var m in _markers.FindOverlappingSegments(viewStart, viewEnd - viewStart))
            yield return m;
    }

    private static void DrawWavyUnderline(TextView textView, DrawingContext dc, TextMarker marker)
    {
        if (marker.Length <= 0)
            return;

        var geoBuilder = new BackgroundGeometryBuilder
        {
            AlignToWholePixels = true,
            BorderThickness = 0
        };

        geoBuilder.AddSegment(textView, new TextSegment { StartOffset = marker.StartOffset, Length = marker.Length });

        var geometry = geoBuilder.CreateGeometry();
        if (geometry is null)
            return;

        // We don’t draw the filled geometry; we use it to obtain bounds and draw a squiggle at the bottom.
        Rect bounds = geometry.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        var pen = new Pen(new SolidColorBrush(marker.Color), 1.4);
        pen.Freeze();

        // Build a wavy underline across the segment bounds.
        // Wave parameters tuned for legibility at typical editor font sizes.
        double y = bounds.Bottom + 1;
        double x0 = bounds.Left;
        double x1 = bounds.Right;

        const double step = 4.0;
        const double amp = 1.5;

        var g = new StreamGeometry();
        using (var ctx = g.Open())
        {
            bool up = true;
            ctx.BeginFigure(new Point(x0, y), false, false);

            for (double x = x0; x < x1; x += step)
            {
                double yy = y + (up ? -amp : amp);
                ctx.LineTo(new Point(Math.Min(x + step, x1), yy), true, false);
                up = !up;
            }
        }
        g.Freeze();

        dc.DrawGeometry(null, pen, g);
    }

    public sealed class TextMarker : TextSegment
    {
        public Color Color { get; init; }
        public string? ToolTip { get; init; }
    }
}