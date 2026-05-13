namespace Core.Rendering.Sppm;

/// <summary>
/// Snapshot of SPPM renderer state reported to the UI after each iteration.
/// </summary>
public sealed record SppmProgress(
    int    Iteration,
    float  AverageRadius,
    long   TotalPhotonsEmitted,
    int    PhotonsPerIteration,
    double ElapsedSeconds,
    double IterationsPerSecond
)
{
    public string FormatStats() =>
        $"SPPM  iter {Iteration}  |  " +
        $"photons {TotalPhotonsEmitted:N0}  |  " +
        $"avg-R {AverageRadius:F3}  |  " +
        $"{IterationsPerSecond:F2} it/s  |  " +
        $"{ElapsedSeconds:F1} s elapsed";
}