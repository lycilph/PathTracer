using Core.Materials;
using Core.Math;

namespace Core.PhotonMapping.Sppm;

/// <summary>
/// A visible point (hit point) generated during the eye pass.
/// For 12.2 we only create these on Lambertian surfaces.
/// </summary>
public sealed class VisiblePoint
{
    public int PixelX { get; init; }
    public int PixelY { get; init; }

    public Vec3 Position { get; init; }
    public Vec3 Normal { get; init; }

    // Throughput from camera to this point (includes any delta chain before it).
    public Vec3 Beta { get; init; }

    public Lambertian Material { get; init; } = null!;

    // Search radius for gathering photons (fixed in 12.2)
    public float Radius { get; set; }

    // Iteration-local accumulators (reset each iteration)
    public int M;          // number of photons found this iteration
    public Vec3 Phi;       // accumulated flux contribution (for photon-only estimate/debug)
}