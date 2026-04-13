using Core.Algebra;
using Core.Geometry;

namespace Engine.PhotonMapping;

/// <summary>
/// Estimates indirect and caustic radiance at a surface point using
/// the photon map density estimation algorithm (§3.11.3).
/// </summary>
public sealed class RadianceEstimator
{
    /// <summary>
    /// Maximum number of photons to use in the radiance estimate.
    /// Higher values reduce noise but increase bias.
    /// </summary>
    public int KNearest { get; init; } = 50;

    /// <summary>
    /// PPM radius contraction factor. Must be in (0, 1].
    /// Spec recommends α ≈ 0.7 (§3.11.4).
    /// </summary>
    public double Alpha { get; init; } = 0.7;

    /// <summary>
    /// Estimates the radiance at a surface point using the k nearest
    /// photons within the current search radius.
    /// </summary>
    /// <param name="hit">The surface intersection point.</param>
    /// <param name="photonMap">The photon map to query.</param>
    /// <param name="state">
    /// The per-pixel PPM state tracking accumulated flux,
    /// photon count and current radius.
    /// </param>
    /// <returns>
    /// Estimated indirect + caustic radiance at this surface point.
    /// </returns>
    public Vector3 Estimate(
        HitRecord hit,
        PhotonMap photonMap,
        ref PixelEstimationState state)
    {
        if (photonMap.Count == 0) return Vector3.Zero;

        // Query k nearest photons within current radius
        var nearest = photonMap.FindNearest(
            hit.Point,
            KNearest,
            state.Radius);

        if (nearest.Count == 0) return Vector3.Zero;

        var M = nearest.Count;

        // Accumulate flux from nearby photons using cone filter
        var flux = Vector3.Zero;
        foreach (var (photon, distSq) in nearest)
        {
            // Only consider photons on the same side of the surface
            if (Vector3.Dot(photon.Direction, hit.Normal) > 0)
                continue;

            // Evaluate the BRDF at this photon's incident direction
            var brdf = EvalLambertianBrdf(hit);

            // Cone filter kernel: K = 1 - dist / (k * r)
            var dist = Math.Sqrt(distSq);
            var kernel = 1 - dist / state.Radius;

            flux = flux + brdf * photon.Power * kernel;
        }

        // §3.11.3 Radiance estimate: L = Σ f_r · Φ · K / (π · r²)
        var radianceEstimate = flux / (Math.PI * state.Radius * state.Radius);

        // ── PPM radius update (§3.11.4) ───────────────────────────────────
        // r_{n+1} = r_n · √((N_n + α·M_n) / (N_n + M_n))
        var N = state.AccumulatedPhotonCount;
        var newN = N + Alpha * M;
        var newRadius = state.Radius
                      * Math.Sqrt(newN / (N + M));

        // Update accumulated flux — scale existing flux by radius ratio
        var radiusRatio = (newRadius * newRadius)
                        / (state.Radius * state.Radius);

        state = new PixelEstimationState
        {
            AccumulatedFlux = (state.AccumulatedFlux + flux) * radiusRatio,
            AccumulatedPhotonCount = newN,
            Radius = newRadius
        };

        return radianceEstimate;
    }

    /// <summary>
    /// Evaluates the Lambertian BRDF f_r = albedo / π at the given
    /// surface point. For non-Lambertian surfaces returns a neutral value.
    /// </summary>
    private static Vector3 EvalLambertianBrdf(HitRecord hit)
    {
        if (hit.Material is Engine.Materials.Lambertian lambertian)
            return lambertian.Albedo / Math.PI;

        // For other diffuse-like materials use a neutral grey
        return new Vector3(0.5, 0.5, 0.5) / Math.PI;
    }
}

/// <summary>
/// Per-pixel state for Progressive Photon Mapping (§3.11.4).
/// Tracks accumulated flux, photon count and current search radius
/// across PPM passes.
/// </summary>
public struct PixelEstimationState
{
    /// <summary>
    /// Accumulated flux from all photon map passes so far.
    /// Units: power (watts per RGB channel).
    /// </summary>
    public Vector3 AccumulatedFlux { get; set; }

    /// <summary>
    /// Accumulated photon count N_n — the effective number of photons
    /// contributing to this pixel after radius contraction.
    /// </summary>
    public double AccumulatedPhotonCount { get; set; }

    /// <summary>
    /// Current search radius r_n. Starts at the initial radius and
    /// shrinks each pass according to the PPM update rule.
    /// </summary>
    public double Radius { get; set; }

    /// <summary>
    /// Creates an initial state with zero flux, zero photon count
    /// and the given starting radius.
    /// </summary>
    public static PixelEstimationState Initial(double initialRadius) =>
        new()
        {
            AccumulatedFlux = Vector3.Zero,
            AccumulatedPhotonCount = 0,
            Radius = initialRadius
        };
}