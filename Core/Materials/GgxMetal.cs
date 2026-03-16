namespace Core.Materials;

/// <summary>
/// A GGX microfacet metallic material (§3.8.3).
/// Models metals with physically correct specular reflection and roughness.
/// </summary>
/// <param name="F0">
/// Reflectance at normal incidence per RGB channel.
/// Silver ≈ (0.95, 0.93, 0.88). Each component in [0,1].
/// </param>
/// <param name="Roughness">
/// Surface roughness α in [0,1]. 0 = perfect mirror, 1 = fully diffuse-looking.
/// </param>
public sealed class GgxMetal(Vector3 F0, double Roughness) : IMaterial
{
    public Vector3 F0 { get; } = F0;
    public double Roughness { get; } = Math.Clamp(Roughness, 0.001, 1.0);

    /// <inheritdoc/>
    /// <remarks>
    /// Samples a micro-facet normal h from the GGX distribution,
    /// reflects the incoming ray about h, then evaluates the full
    /// D·G·F BRDF weight divided by the sampling PDF.
    /// </remarks>
    public bool Scatter(Ray rayIn, HitRecord hit, Sampler sampler,
                        out Vector3 attenuation, out Ray scattered)
    {
        attenuation = Vector3.Zero;
        scattered = default;

        var v = -rayIn.Direction; // view direction (points away from surface)

        // Sample a micro-facet normal from the GGX distribution
        var h = SampleGgxNormal(hit.Normal, sampler);

        // Reflect view direction about the micro-facet normal to get light direction
        var l = Mirror.Reflect(-v, h);

        // Reject if the scattered ray goes below the surface
        var nDotL = Vector3.Dot(hit.Normal, l);
        var nDotV = Vector3.Dot(hit.Normal, v);
        if (nDotL <= 0 || nDotV <= 0)
            return false;

        var nDotH = Math.Max(Vector3.Dot(hit.Normal, h), 0.0);
        var vDotH = Math.Max(Vector3.Dot(v, h), 0.0);

        // Evaluate BRDF terms
        var d = DistributionGgx(nDotH);
        var g = GeometrySmith(nDotV, nDotL);
        var f = FresnelSchlick(vDotH, F0);

        // PDF of sampling h from GGX: p(h) = D(h)·(n·h)
        // After change of variables to l: p(l) = D(h)·(n·h) / (4·(v·h))
        // Full weight: f_r / p(l) = (D·G·F / 4·nDotL·nDotV) / (D·nDotH / 4·vDotH)
        //                         = G·F·vDotH / (nDotV·nDotH)
        var weight = g * vDotH / (nDotV * nDotH);

        attenuation = f * weight;
        scattered = new Ray(hit.Point, l.Normalize());
        return true;
    }

    /// <summary>
    /// Samples a micro-facet normal from the GGX distribution oriented
    /// around <paramref name="normal"/> using spherical coordinates.
    /// </summary>
    private Vector3 SampleGgxNormal(Vector3 normal, Sampler sampler)
    {
        var r1 = sampler.Next();
        var r2 = sampler.Next();
        var a = Roughness;

        // GGX importance sampling: θ_h = arctan(α√ξ₁/√(1−ξ₁))
        var theta = Math.Atan(a * Math.Sqrt(r1) / Math.Sqrt(1.0 - r1));
        var phi = 2.0 * Math.PI * r2;

        var sinTheta = Math.Sin(theta);
        var cosTheta = Math.Cos(theta);

        var localH = new Vector3(
            sinTheta * Math.Cos(phi),
            sinTheta * Math.Sin(phi),
            cosTheta);

        return ToWorld(localH, normal);
    }

    /// <summary>
    /// GGX Normal Distribution Function D(h) (§3.8.3).
    /// D(h) = α² / (π·((n·h)²·(α²−1)+1)²)
    /// </summary>
    private double DistributionGgx(double nDotH)
    {
        var a2 = Roughness * Roughness;
        var denom = nDotH * nDotH * (a2 - 1.0) + 1.0;
        return a2 / (Math.PI * denom * denom);
    }

    /// <summary>
    /// Smith geometry masking-shadowing function G(ω_i, ω_o) (§3.8.3).
    /// G = G1(nDotV) · G1(nDotL),  G1(x) = x / (x·(1−k) + k),  k = α²/2
    /// </summary>
    private double GeometrySmith(double nDotV, double nDotL)
    {
        var k = Roughness * Roughness / 2.0;
        var g1 = nDotV / (nDotV * (1.0 - k) + k);
        var g2 = nDotL / (nDotL * (1.0 - k) + k);
        return g1 * g2;
    }

    /// <summary>
    /// Schlick Fresnel approximation for metals (§3.8.3).
    /// F(θ) = F0 + (1−F0)·(1−cosθ)⁵
    /// </summary>
    private static Vector3 FresnelSchlick(double cosTheta, Vector3 f0)
    {
        var t = Math.Pow(1.0 - cosTheta, 5);
        return f0 + (Vector3.One - f0) * t;
    }

    /// <summary>Transforms a local-frame direction into world space (same ONB as Sampler).</summary>
    private static Vector3 ToWorld(Vector3 local, Vector3 normal)
    {
        Vector3 tangent;
        if (Math.Abs(normal.X) > 0.9)
            tangent = Vector3.Cross(new Vector3(0, 1, 0), normal).Normalize();
        else
            tangent = Vector3.Cross(new Vector3(1, 0, 0), normal).Normalize();

        var bitangent = Vector3.Cross(normal, tangent);
        return (local.X * tangent + local.Y * bitangent + local.Z * normal).Normalize();
    }
}