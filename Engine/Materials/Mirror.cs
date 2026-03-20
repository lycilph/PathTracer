using Core;
using Core.Algebra;
using Core.Geometry;
using Core.Sampling;

namespace Engine.Materials;

/// <summary>
/// A perfect specular mirror (§3.8.2).
/// Reflects incoming rays deterministically — no sampling variance.
/// </summary>
/// <param name="Reflectance">
/// Fraction of light reflected per RGB channel. Each component in [0,1].
/// </param>
public sealed class Mirror(Vector3 Reflectance) : IMaterial
{
    public Vector3 Reflectance { get; } = Reflectance;

    /// <inheritdoc/>
    /// <remarks>
    /// Reflected direction: ω_r = d − 2(d·n)n (§3.8.2).
    /// PDF = 1, so attenuation = reflectance exactly.
    /// </remarks>
    public bool Scatter(Ray rayIn, HitRecord hit, Sampler sampler,
                        out Vector3 attenuation, out Ray scattered)
    {
        var reflected = Reflect(rayIn.Direction, hit.Normal);

        attenuation = Reflectance;
        scattered = new Ray(hit.Point, reflected);

        // Only scatter if the reflected ray is in the correct hemisphere
        return Vector3.Dot(reflected, hit.Normal) > 0;
    }

    /// <summary>
    /// Computes the reflection of direction <paramref name="d"/> about
    /// normal <paramref name="n"/>: r = d − 2(d·n)n.
    /// </summary>
    /// <param name="d">Incident direction. Need not be unit length.</param>
    /// <param name="n">Surface normal. Must be unit length.</param>
    public static Vector3 Reflect(Vector3 d, Vector3 n)
        => d - 2.0 * Vector3.Dot(d, n) * n;

    /// <summary>
    /// Returns the PDF of scattering in direction <paramref name="scattered"/>.
    /// </summary>
    /// <remarks>
    /// A perfect mirror is a delta distribution — there is exactly one valid
    /// outgoing direction. By convention delta distributions return 1.0, which
    /// signals the MIS integrator to skip light sampling for this bounce (§3.8.2).
    /// </remarks>
    public double Pdf(Ray rayIn, HitRecord hit, Ray scattered) => 1.0;

    /// <inheritdoc/>
    /// <remarks>
    /// Delta distribution — no valid evaluation at arbitrary directions.
    /// </remarks>
    public Vector3 Evaluate(Ray rayIn, HitRecord hit, Ray scattered)
        => Vector3.Zero;
}