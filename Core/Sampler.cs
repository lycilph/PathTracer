namespace Core;

/// <summary>
/// Per-thread random number generation and directional sampling utilities (§3.7).
/// Each render thread should own its own Sampler instance to avoid lock contention.
/// </summary>
public sealed class Sampler
{
    private readonly Random _rng;

    /// <param name="seed">
    /// Explicit seed for reproducibility. Different threads should use different seeds.
    /// </param>
    public Sampler(int seed) => _rng = new Random(seed);

    /// <summary>Returns a uniform random double in [0, 1).</summary>
    public double Next() => _rng.NextDouble();

    /// <summary>Returns a uniform random double in [min, max).</summary>
    public double Next(double min, double max) => min + (max - min) * Next();

    /// <summary>
    /// Samples a direction from a cosine-weighted hemisphere distribution (§3.7.2).
    /// The returned direction is in world space, oriented around <paramref name="normal"/>.
    /// </summary>
    /// <param name="normal">The surface normal defining the hemisphere. Must be unit length.</param>
    /// <returns>A unit vector sampled with PDF = cosθ/π.</returns>
    /// <remarks>
    /// Uses Malley's method: sample a point on a unit disk then project up to the hemisphere.
    /// cos θ = √ξ₁  →  directions near the normal are sampled more frequently.
    /// </remarks>
    public Vector3 CosineWeightedHemisphere(Vector3 normal)
    {
        var r1 = Next();
        var r2 = Next();

        // §3.7.2 — cosine-weighted direction in local frame (Z = up)
        var cosTheta = Math.Sqrt(r1);
        var sinTheta = Math.Sqrt(1.0 - r1);
        var phi = 2.0 * Math.PI * r2;

        var localDir = new Vector3(
            sinTheta * Math.Cos(phi),
            sinTheta * Math.Sin(phi),
            cosTheta);

        return ToWorld(localDir, normal);
    }

    /// <summary>
    /// Transforms a direction from the local frame (Z aligned with <paramref name="normal"/>)
    /// into world space using an orthonormal basis.
    /// </summary>
    private static Vector3 ToWorld(Vector3 localDir, Vector3 normal)
    {
        // Build an orthonormal basis around the normal (Frisvad / Hughes-Möller method)
        Vector3 tangent;

        if (Math.Abs(normal.X) > 0.9)
            tangent = Vector3.Cross(new Vector3(0, 1, 0), normal).Normalize();
        else
            tangent = Vector3.Cross(new Vector3(1, 0, 0), normal).Normalize();

        var bitangent = Vector3.Cross(normal, tangent);

        // Express localDir in world space
        return localDir.X * tangent +
               localDir.Y * bitangent +
               localDir.Z * normal;
    }
}