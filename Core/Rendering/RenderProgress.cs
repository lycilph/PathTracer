namespace Core.Rendering;

public readonly record struct RenderProgress(
    int Width,
    int Height,
    int SamplesPerPixelMin,     // minimum samples accumulated among pixels (for partial validity)
    int SamplesPerPixelMax,     // maximum samples accumulated among pixels
    int TilesDone,
    int TilesTotal,
    double ElapsedSeconds,
    double SamplesPerSecond,
    double MsPerTile,
    float AverageLuminance);
