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

    public Vec3 Position { get; set; }
    public Vec3 Normal { get; set; }

    // Throughput from camera to this point (includes any delta chain before it).
    public Vec3 Beta { get; set; }

    public Lambertian Material { get; set; } = null!;

    // --- Progressive SPPM state ---
    public float Radius;     // R_i
    public float N;          // accumulated photon count
    public Vec3 Tau;         // accumulated flux τ_i

    // --- Iteration-local (reset every iteration) ---
    public int M;            // photons this iteration
    public Vec3 Phi;         // flux this iteration
}