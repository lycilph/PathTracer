using Core.Algebra;

namespace ScriptApi;

/// <summary>
/// Groups all rendering configuration that is independent of scene geometry.
/// </summary>
public sealed class RenderSettings
{
    /// <summary>Output image width in pixels.</summary>
    public int ImageWidth { get; }

    /// <summary>Output image height in pixels.</summary>
    public int ImageHeight { get; }

    /// <summary>Number of samples per pixel.</summary>
    public int SamplesPerPixel { get; }

    /// <summary>
    /// Radiance returned when a ray escapes the scene without hitting
    /// any geometry. Use Vector3.Zero for a black background.
    /// </summary>
    public Vector3 BackgroundRadiance { get; }

    internal RenderSettings(
        int imageWidth,
        int imageHeight,
        int samplesPerPixel,
        Vector3 backgroundRadiance)
    {
        ImageWidth = imageWidth;
        ImageHeight = imageHeight;
        SamplesPerPixel = samplesPerPixel;
        BackgroundRadiance = backgroundRadiance;
    }
}
