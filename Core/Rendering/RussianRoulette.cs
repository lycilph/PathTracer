using Core.Math;

namespace Core.Rendering;

/// <summary>
/// Russian roulette termination helpers.
/// Used to terminate low-throughput paths without introducing bias.
/// </summary>
public static class RussianRoulette
{
    /// <summary>
    /// Returns a continuation probability based on path throughput.
    /// We use the max RGB component (common choice for RGB renderers) and clamp it.
    /// </summary>
    public static float ContinuationProbability(in Vec3 throughput, float min = 0.05f, float max = 0.95f)
    {
        float p = throughput.X;
        if (throughput.Y > p) p = throughput.Y;
        if (throughput.Z > p) p = throughput.Z;
        return MathUtil.Clamp(p, min, max);
    }
}