using Core;
using Core.Algebra;
using Core.Geometry;
using Core.Sampling;

namespace Engine.Materials;

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

    /// <summary>
    /// Returns the PDF of scattering in direction <paramref name="scattered"/>.
    /// </summary>
    /// <remarks>
    /// Cosine-weighted hemisphere PDF: p(ω) = cosθ / π (§3.7.2).
    /// Matches the importance sampling used in <see cref="Scatter"/>, so the
    /// estimator weight f_r·cosθ/p(ω) simplifies to albedo exactly.
    /// </remarks>
    public double Pdf(Ray rayIn, HitRecord hit, Ray scattered)
    {
        // Cosine-weighted hemisphere PDF: p(ω) = cosθ / π  (§3.7.2)
        var cosTheta = Vector3.Dot(hit.Normal, scattered.Direction);
        return Math.Max(0, cosTheta / Math.PI);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Lambertian BRDF is constant: f_r = albedo / π (§3.8.1).
    /// </remarks>
    public Vector3 Evaluate(Ray rayIn, HitRecord hit, Ray scattered)
    {
        var cosTheta = Vector3.Dot(hit.Normal, scattered.Direction);
        if (cosTheta <= 0) return Vector3.Zero;
        return Albedo / Math.PI;
    }
}