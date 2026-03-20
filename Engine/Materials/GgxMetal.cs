using Core;
using Core.Algebra;
using Core.Geometry;
using Core.Sampling;

namespace Engine.Materials;

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
        var h = SampleGgxNormal(hit.Normal, v, sampler);

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
    /// Samples a micro-facet normal from the GGX Visible Normal Distribution
    /// Function (VNDF) oriented around <paramref name="normal"/>.
    /// </summary>
    /// <remarks>
    /// VNDF sampling (Heitz 2018) only produces normals visible from the view
    /// direction, eliminating below-surface samples and reducing variance
    /// significantly at low roughness values compared to basic GGX sampling.
    /// </remarks>
    private Vector3 SampleGgxNormal(Vector3 normal, Vector3 v, Sampler sampler)
    {
        var r1 = sampler.Next();
        var r2 = sampler.Next();
        var a = Roughness;

        // Transform view direction to local space
        var localV = ToLocal(v, normal);

        // Stretch view direction by roughness
        var stretchedV = new Vector3(a * localV.X, a * localV.Y, localV.Z).Normalize();

        // Build orthonormal basis around stretched view direction
        var t1 = stretchedV.Z < 0.9999
            ? Vector3.Cross(stretchedV, Vector3.UnitZ).Normalize()
            : Vector3.UnitX;
        var t2 = Vector3.Cross(t1, stretchedV);

        // Sample point on the hemisphere
        var a2 = 1.0 / (1.0 + stretchedV.Z);
        var r = Math.Sqrt(r1);
        var phi = r2 < a2
            ? r2 / a2 * Math.PI
            : Math.PI + (r2 - a2) / (1.0 - a2) * Math.PI;

        var p1 = r * Math.Cos(phi);
        var p2 = r * Math.Sin(phi) * (r2 < a2 ? 1.0 : stretchedV.Z);

        // Compute normal in stretched space
        var p3 = Math.Sqrt(Math.Max(0.0, 1.0 - p1 * p1 - p2 * p2));
        var localH = p1 * t1 + p2 * t2 + p3 * stretchedV;

        // Unstretch and return in world space
        var unstretched = new Vector3(a * localH.X, a * localH.Y, Math.Max(0.0, localH.Z)).Normalize();
        return ToWorld(unstretched, normal);
    }

    /// <summary>
    /// Transforms a world-space direction into the local frame defined by
    /// <paramref name="normal"/>.
    /// </summary>
    private static Vector3 ToLocal(Vector3 v, Vector3 normal)
    {
        Vector3 tangent;
        if (Math.Abs(normal.X) > 0.9)
            tangent = Vector3.Cross(new Vector3(0, 1, 0), normal).Normalize();
        else
            tangent = Vector3.Cross(new Vector3(1, 0, 0), normal).Normalize();

        var bitangent = Vector3.Cross(normal, tangent);
        return new Vector3(
            Vector3.Dot(v, tangent),
            Vector3.Dot(v, bitangent),
            Vector3.Dot(v, normal));
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

    /// <summary>
    /// Returns the PDF of scattering in direction <paramref name="scattered"/>.
    /// </summary>
    /// <remarks>
    /// The GGX visible normal distribution PDF after change of variables from
    /// half-vector h to outgoing direction l (§3.8.3):
    /// p(l) = D(h)·(n·h) / (4·(v·h))
    /// where h = normalize(v + l) is the half vector between view and light.
    /// </remarks>
    public double Pdf(Ray rayIn, HitRecord hit, Ray scattered)
    {
        var v = -rayIn.Direction;
        var l = scattered.Direction;
        var h = (v + l).Normalize();               // half vector
        var nDotH = Math.Max(Vector3.Dot(hit.Normal, h), 0.0);
        var vDotH = Math.Max(Vector3.Dot(v, h), 0.0);

        // p(l) = D(h)·(n·h) / (4·(v·h))
        return DistributionGgx(nDotH) * nDotH / (4.0 * vDotH + 1e-10);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Evaluates the full GGX BRDF: D·G·F / (4·nDotL·nDotV) (§3.8.3).
    /// </remarks>
    public Vector3 Evaluate(Ray rayIn, HitRecord hit, Ray scattered)
    {
        var v = -rayIn.Direction;
        var l = scattered.Direction;
        var h = (v + l).Normalize();

        var nDotL = Vector3.Dot(hit.Normal, l);
        var nDotV = Vector3.Dot(hit.Normal, v);
        if (nDotL <= 0 || nDotV <= 0) return Vector3.Zero;

        var nDotH = Math.Max(Vector3.Dot(hit.Normal, h), 0.0);
        var vDotH = Math.Max(Vector3.Dot(v, h), 0.0);

        var d = DistributionGgx(nDotH);
        var g = GeometrySmith(nDotV, nDotL);
        var f = FresnelSchlick(vDotH, F0);

        return f * (d * g / (4.0 * nDotL * nDotV + 1e-10));
    }
}