namespace Core.Materials;

/// <summary>
/// Ideal diffuse (Lambertian) material (§3.8.1).
/// Scatters incoming light equally in all directions, weighted by cosθ.
/// </summary>
/// <param name="Albedo">
/// Fraction of light reflected per RGB channel. Each component in [0,1].
/// </param>
public sealed class Lambertian(Vector3 Albedo) : IMaterial
{
    public Vector3 Albedo { get; } = Albedo;

    /// <inheritdoc/>
    /// <remarks>
    /// Uses cosine-weighted hemisphere sampling (§3.7.2).
    /// The PDF (cosθ/π) cancels with the BRDF (albedo/π) and cosθ factor,
    /// leaving attenuation = albedo exactly.
    /// </remarks>
    public bool Scatter(Ray rayIn, HitRecord hit, Sampler sampler,
                        out Vector3 attenuation, out Ray scattered)
    {
        var direction = sampler.CosineWeightedHemisphere(hit.Normal);

        // Guard against degenerate scatter direction (very unlikely but possible)
        if (direction.IsNearZero())
            direction = hit.Normal;

        scattered = new Ray(hit.Point, direction);
        attenuation = Albedo;
        return true;
    }
}