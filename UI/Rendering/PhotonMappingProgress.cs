using Engine.Rendering;

namespace UI.Rendering;

/// <summary>
/// Progress snapshot delivered after each PPM pass completes.
/// </summary>
public sealed class PhotonMappingProgress
{
    /// <summary>The current pass number (1-based).</summary>
    public int Pass { get; }

    /// <summary>Number of photons stored in this pass.</summary>
    public int PhotonsThisPass { get; }

    /// <summary>Total photons emitted across all passes so far.</summary>
    public long TotalPhotons { get; }

    /// <summary>
    /// Average search radius across all pixels after this pass.
    /// Decreases each pass as PPM converges.
    /// </summary>
    public double AverageRadius { get; }

    /// <summary>
    /// The frame buffer accumulating radiance.
    /// Safe to read from the callback.
    /// </summary>
    public FrameBuffer FrameBuffer { get; }

    /// <summary>Time elapsed since rendering started.</summary>
    public TimeSpan Elapsed { get; }

    public PhotonMappingProgress(
        int pass,
        int photonsThisPass,
        long totalPhotons,
        double averageRadius,
        FrameBuffer frameBuffer,
        TimeSpan elapsed)
    {
        Pass = pass;
        PhotonsThisPass = photonsThisPass;
        TotalPhotons = totalPhotons;
        AverageRadius = averageRadius;
        FrameBuffer = frameBuffer;
        Elapsed = elapsed;
    }
}