using Core;
using Core.Algebra;
using Core.Geometry;
using Core.Sampling;
using Engine.PhotonMapping;

namespace Engine.Integrators;

/// <summary>
/// Path integrator that combines MIS direct lighting with photon map
/// density estimation for indirect and caustic illumination (§3.11).
/// Direct lighting is handled by <see cref="MisIntegrator"/>.
/// Indirect and caustic contributions come from the photon map.
/// </summary>
public sealed class PhotonMapIntegrator
{
    private readonly MisIntegrator _directLightingIntegrator;
    private readonly RadianceEstimator _radianceEstimator;

    /// <summary>Radiance returned when a ray escapes the scene.</summary>
    public Vector3 BackgroundRadiance { get; init; } = Vector3.Zero;

    /// <param name="kNearest">
    /// Number of photons to use in the radiance estimate.
    /// </param>
    /// <param name="alpha">
    /// PPM radius contraction factor. Spec recommends ≈ 0.7.
    /// </param>
    public PhotonMapIntegrator(int kNearest = 50, double alpha = 0.7)
    {
        _directLightingIntegrator = new MisIntegrator
        {
            BackgroundRadiance = BackgroundRadiance,
            MaxDepth = 3  // Shallow depth — photon map handles deep paths
        };

        _radianceEstimator = new RadianceEstimator
        {
            KNearest = kNearest,
            Alpha = alpha
        };
    }

    /// <summary>
    /// Traces a ray and estimates the total radiance — direct via MIS
    /// and indirect/caustic via the photon map.
    /// </summary>
    /// <param name="ray">The primary ray to trace.</param>
    /// <param name="scene">The scene to trace against.</param>
    /// <param name="lights">Samplable lights for direct lighting.</param>
    /// <param name="photonMap">The current photon map.</param>
    /// <param name="pixelStates">
    /// Per-pixel PPM state array indexed by pixel index.
    /// </param>
    /// <param name="pixelIndex">
    /// Index into <paramref name="pixelStates"/> for this ray.
    /// </param>
    /// <param name="sampler">Per-thread sampler.</param>
    public Vector3 Trace(
        Ray ray,
        IHittable scene,
        IReadOnlyList<ILight> lights,
        PhotonMap photonMap,
        PixelEstimationState[] pixelStates,
        int pixelIndex,
        Sampler sampler)
    {
        // Find the first visible surface point
        if (!scene.Hit(ray, out var hit))
            return BackgroundRadiance;

        // Hit an emissive surface — return emission directly
        if (hit.Material is Materials.Emissive emissive)
            return emissive.Emit();

        // ── Direct lighting via MIS ───────────────────────────────────────
        var direct = _directLightingIntegrator.Trace(
            ray, scene, lights, sampler);

        // ── Indirect + caustic via photon map ─────────────────────────────
        // Only estimate at diffuse surfaces
        if (hit.Material is not Materials.Lambertian)
            return direct;

        ref var state = ref pixelStates[pixelIndex];
        var indirect = _radianceEstimator.Estimate(hit, photonMap,
                                                    ref state);

        return direct + indirect;
    }

    /// <summary>
    /// Finds the first visible diffuse surface point for a ray.
    /// Used by the PPM renderer to build the visible point map.
    /// </summary>
    /// <returns>
    /// The hit record of the first diffuse surface, or null if the ray
    /// escapes or hits a non-diffuse surface.
    /// </returns>
    public HitRecord? FindVisibleDiffusePoint(Ray ray, IHittable scene)
    {
        var currentRay = ray;
        var depth = 0;
        const int maxSpecularDepth = 10;

        while (depth < maxSpecularDepth)
        {
            if (!scene.Hit(currentRay, out var hit))
                return null;

            // Found a diffuse surface
            if (hit.Material is Materials.Lambertian)
                return hit;

            // Skip emissive surfaces
            if (hit.Material is Materials.Emissive)
                return null;

            // Follow specular surfaces (mirror, glass)
            // to find the underlying diffuse surface
            var sampler = new Sampler(depth);
            if (!hit.Material.Scatter(currentRay, hit, sampler,
                    out _, out var scattered))
                return null;

            currentRay = scattered;
            depth++;
        }

        return null;
    }
}
