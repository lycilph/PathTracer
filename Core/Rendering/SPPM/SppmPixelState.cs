using Core.Math;
using Core.Scene;

namespace Core.Rendering.Sppm;

/// <summary>
/// Per-pixel state for Stochastic Progressive Photon Mapping.
/// Two kinds of data live here:
///   1. Current-iteration camera hit point (overwritten each pass).
///   2. Progressive accumulation (N, R, Tau) that persists across all iterations.
/// </summary>
public sealed class SppmPixelState
{
    // ── Current camera-ray hit point (written during camera pass) ─────────────

    /// <summary>True when the camera ray found a non-specular (diffuse/rough) surface.</summary>
    public bool IsValid;

    public Vec3 Position;
    public Vec3 Normal;

    /// <summary>Outgoing (view) direction at the hit point, pointing toward camera.</summary>
    public Vec3 Wo;

    /// <summary>Full hit record needed for BSDF evaluation during gather.</summary>
    public HitRecord Hit;

    /// <summary>Accumulated path throughput (reflectance × transmittance) from the camera lens to this hit point.</summary>
    public Vec3 CameraPathThroughput;

    /// <summary>
    /// Direct-lighting contribution for this iteration: emission seen along the path
    /// plus next-event-estimation at the diffuse hit point.
    /// Averaged into DirectLightSum across iterations.
    /// </summary>
    public Vec3 DirectLight;

    // ── Progressive accumulation (persists across iterations) ────────────────

    /// <summary>Current search radius (shrinks each iteration via the SPPM formula).</summary>
    public float Radius;

    /// <summary>Accumulated α-weighted photon count. Floating-point to support fractional alpha.</summary>
    public double N;

    /// <summary>
    /// Accumulated flux estimate τ.  Progressive formula ensures
    ///   τ_{k+1} = (τ_k + ΔΦ_k) × (R_{k+1}² / R_k²)
    /// Final indirect radiance = τ / (π R² × total_photons_emitted).
    /// </summary>
    public Vec3 Tau;

    // ── Running direct-light average ──────────────────────────────────────────

    /// <summary>Sum of per-iteration direct-light estimates (divided by count for display).</summary>
    public Vec3 DirectLightSum;

    /// <summary>Number of iterations that contributed to DirectLightSum.</summary>
    public int DirectLightIterations;

    // ── Per-iteration gather scratch (reset at the start of each camera pass) ─

    /// <summary>ΔN: raw count of photons within Radius during this iteration's gather.</summary>
    public int NewPhotons;

    /// <summary>
    /// ΔΦ: accumulated flux from photons within Radius this iteration,
    /// already weighted by CameraPathThroughput and the BSDF at the hit point.
    /// </summary>
    public Vec3 NewFlux;
}