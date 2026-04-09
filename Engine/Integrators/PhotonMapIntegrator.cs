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
    private MisIntegrator? _directLightingIntegrator;
    private readonly RadianceEstimator _radianceEstimator;

    private MisIntegrator DirectLightingIntegrator =>
    _directLightingIntegrator ??= new MisIntegrator
    {
        BackgroundRadiance = BackgroundRadiance,
        MaxDepth = 10
    };

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
        _radianceEstimator = new RadianceEstimator
        {
            KNearest = kNearest,
            Alpha = alpha
        };
    }

    /// <summary>
    /// Traces direct lighting only using MIS.
    /// Called once per pixel per pass and accumulated across passes.
    /// </summary>
    public Vector3 TraceDirect(
        Ray ray,
        IHittable scene,
        IReadOnlyList<ILight> lights,
        Sampler sampler)
    {
        if (!scene.Hit(ray, out var hit))
            return BackgroundRadiance;

        // Hit an emissive surface
        if (hit.Material is Materials.Emissive emissive)
            return emissive.Emit();

        // Only compute direct lighting at diffuse surfaces
        if (hit.Material is not Materials.Lambertian)
            return DirectLightingIntegrator.Trace(ray, scene, lights, sampler);

        return DirectLightingIntegrator.Trace(ray, scene, lights, sampler);
    }

    /// <summary>
    /// Estimates indirect + caustic radiance only using the photon map.
    /// Called once per pixel per pass — replaces previous indirect estimate.
    /// </summary>
    public Vector3 TraceIndirect(
        Ray ray,
        IHittable scene,
        PhotonMap photonMap,
        PixelEstimationState[] pixelStates,
        int pixelIndex,
        Sampler sampler)
    {
        var hit = FindVisibleDiffusePoint(ray, scene, sampler);
        if (hit is null) return Vector3.Zero;

        ref var state = ref pixelStates[pixelIndex];
        return _radianceEstimator.Estimate(hit.Value, photonMap, ref state);
    }

    /// <summary>
    /// Combined trace — direct + indirect. Used for single-pass rendering.
    /// </summary>
    public Vector3 Trace(
        Ray ray,
        IHittable scene,
        IReadOnlyList<ILight> lights,
        PhotonMap photonMap,
        PixelEstimationState[] pixelStates,
        int pixelIndex,
        Sampler sampler)
    {
        var direct = TraceDirect(ray, scene, lights, sampler);
        var indirect = TraceIndirect(ray, scene, photonMap,
                                     pixelStates, pixelIndex, sampler);
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
    public HitRecord? FindVisibleDiffusePoint(Ray ray, IHittable scene, Sampler sampler)
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
            if (!hit.Material.Scatter(currentRay, hit, sampler,
                    out _, out var scattered))
                return null;

            currentRay = scattered;
            depth++;
        }

        return null;
    }
}
