using Core;
using Core.Algebra;
using Core.Geometry;
using Core.Sampling;

namespace Engine.Materials;

/// <summary>
/// A dielectric (glass) material using Snell's law refraction and
/// Schlick Fresnel reflection blending (§3.8.4).
/// </summary>
/// <param name="Ior">
/// Index of refraction. Air = 1.0, borosilicate glass ≈ 1.5, diamond ≈ 2.4.
/// </param>
public sealed class Dielectric(double Ior) : IMaterial
{
    public double Ior { get; } = Ior;

    /// <inheritdoc/>
    /// <remarks>
    /// At each intersection we either reflect or refract stochastically.
    /// Attenuation is always (1,1,1) — glass absorbs no light.
    /// Both branches set PDF = 1, so throughput is unchanged.
    /// </remarks>
    public bool Scatter(Ray rayIn, HitRecord hit, Sampler sampler,
                        out Vector3 attenuation, out Ray scattered)
    {
        attenuation = Vector3.One; // glass absorbs nothing

        // Determine which side of the surface we are on
        var etaRatio = hit.FrontFace ? (1.0 / Ior) : Ior;
        var cosTheta = Math.Min(Vector3.Dot(-rayIn.Direction, hit.Normal), 1.0);
        var sinTheta = Math.Sqrt(1.0 - cosTheta * cosTheta);

        // §3.8.4 Total Internal Reflection: no real refraction solution exists
        var tir = etaRatio * sinTheta > 1.0;

        Vector3 direction;
        if (tir || Schlick(cosTheta, etaRatio) > sampler.Next())
            direction = Mirror.Reflect(rayIn.Direction, hit.Normal);
        else
            direction = Refract(rayIn.Direction, hit.Normal, etaRatio);

        scattered = new Ray(hit.Point, direction);
        return true;
    }

    /// <summary>
    /// Refracts direction <paramref name="d"/> through a surface with normal
    /// <paramref name="n"/> and relative IOR <paramref name="etaRatio"/> = η_i/η_t.
    /// </summary>
    /// <remarks>
    /// Snell's law in vector form (§3.8.4):
    /// r_perp = etaRatio * (d + cosθ·n)
    /// r_par  = -√(1 - |r_perp|²) · n
    /// </remarks>
    public static Vector3 Refract(Vector3 d, Vector3 n, double etaRatio)
    {
        var cosTheta = Math.Min(Vector3.Dot(-d, n), 1.0);
        var rPerp = etaRatio * (d + cosTheta * n);
        var rPar = -Math.Sqrt(Math.Abs(1.0 - rPerp.LengthSquared)) * n;
        return (rPerp + rPar).Normalize();
    }

    /// <summary>
    /// Schlick approximation for Fresnel reflectance (§3.8.4).
    /// F(θ) = F0 + (1−F0)(1−cosθ)⁵,  F0 = ((η_i−η_t)/(η_i+η_t))²
    /// </summary>
    /// <param name="cosTheta">Cosine of the angle of incidence.</param>
    /// <param name="etaRatio">Relative IOR η_i/η_t.</param>
    /// <returns>Probability of reflection in [0, 1].</returns>
    public static double Schlick(double cosTheta, double etaRatio)
    {
        var f0 = (1.0 - etaRatio) / (1.0 + etaRatio);
        f0 = f0 * f0;
        var base_ = 1.0 - cosTheta;
        return f0 + (1.0 - f0) * base_ * base_ * base_ * base_ * base_;
    }

    /// <summary>
    /// Returns the PDF of scattering in direction <paramref name="scattered"/>.
    /// </summary>
    /// <remarks>
    /// A dielectric is a delta distribution — at each intersection either
    /// reflection or refraction is chosen deterministically given the random
    /// seed. By convention delta distributions return 1.0, signalling the MIS
    /// integrator to skip light sampling for this bounce (§3.8.4).
    /// </remarks>
    public double Pdf(Ray rayIn, HitRecord hit, Ray scattered) => 1.0;

    /// <inheritdoc/>
    /// <remarks>
    /// Delta distribution — no valid evaluation at arbitrary directions.
    /// </remarks>
    public Vector3 Evaluate(Ray rayIn, HitRecord hit, Ray scattered)
        => Vector3.Zero;
}