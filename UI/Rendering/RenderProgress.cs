using Engine.Rendering;

namespace UI.Rendering;

/// <summary>
/// Snapshot of rendering progress delivered to the progress callback
/// after each tile completes.
/// </summary>
public sealed class RenderProgress
{
    /// <summary>Number of tiles completed so far.</summary>
    public int TilesCompleted { get; }

    /// <summary>Total number of tiles in the image.</summary>
    public int TotalTiles { get; }

    /// <summary>
    /// The frame buffer accumulating radiance samples.
    /// Safe to read from the callback — individual pixel reads are
    /// thread-safe.
    /// </summary>
    public FrameBuffer FrameBuffer { get; }

    /// <summary>Percentage of tiles completed in [0, 100].</summary>
    public double PercentComplete =>
        TotalTiles == 0 ? 0 : 100.0 * TilesCompleted / TotalTiles;

    /// <summary>Time elapsed since rendering started.</summary>
    public TimeSpan Elapsed { get; }

    internal RenderProgress(
        int tilesCompleted,
        int totalTiles,
        FrameBuffer frameBuffer,
        TimeSpan elapsed)
    {
        TilesCompleted = tilesCompleted;
        TotalTiles = totalTiles;
        FrameBuffer = frameBuffer;
        Elapsed = elapsed;
    }
}