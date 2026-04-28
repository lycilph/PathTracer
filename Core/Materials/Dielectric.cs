using Core.Math;
using Core.Sampling;
using Core.Scene;

namespace Core.Materials;

/// <summary>
/// Ideal dielectric (glass) with Fresnel reflection + refraction (delta BSDF),
/// plus optional absorption/tint via Beer-Lambert law.
///
/// Tint model:
///   sigmaA = -ln(tint) * absorptionStrength
///   T(d)   = exp(-sigmaA * d)
///
/// If tint is Vec3.One or absorptionStrength=0, glass is clear.
/// </summary>
public sealed class Dielectric : IMaterial
{
    public float Ior { get; }

    // Absorption coefficient sigma_a (RGB). Units: 1 / sceneDistanceUnit
    public Vec3 SigmaA { get; }

    public bool IsDelta => true;

    /// <summary>
    /// Clear glass by default (no absorption).
    /// </summary>
    public Dielectric(float ior = 1.5f)
    {
        Ior = ior;
        SigmaA = Vec3.Zero;
    }

    /// <summary>
    /// Tinted glass.
    ///
    /// tint: desired per-channel transmittance for a reference distance of 1 unit (before scaling).
    /// absorptionStrength: scales absorption to match scene units (Cornell is large, so ~0.01 is a good start).
    /// </summary>
    public Dielectric(float ior, in Vec3 tint, float absorptionStrength)
    {
        Ior = ior;
        SigmaA = ComputeSigmaA(tint, absorptionStrength);
    }

    public Vec3 Emitted(in Ray rayIn, in HitRecord hit) => Vec3.Zero;

    public Vec3 Evaluate(in Vec3 wo, in Vec3 wi, in HitRecord hit) => Vec3.Zero;

    public float Pdf(in Vec3 wo, in Vec3 wi, in HitRecord hit) => 0f;

    public bool Sample(in Vec3 wo, in HitRecord hit, Sampler sampler, out Vec3 wi, out float pdf, out Vec3 f)
    {
        Vec3 incident = (-wo).Normalized();

        float etaI = 1f;
        float etaT = Ior;

        // HitRecord orients Normal against incident for front faces.
        // If we are exiting, swap etaI/etaT.
        if (!hit.FrontFace)
        {
            etaI = Ior;
            etaT = 1f;
        }

        float eta = etaI / etaT;

        float cosTheta = float.Min(Vec3.Dot(-incident, hit.Normal), 1f);
        float sinTheta = float.Sqrt(float.Max(0f, 1f - cosTheta * cosTheta));

        bool cannotRefract = eta * sinTheta > 1f; // TIR
        float reflectProb = cannotRefract ? 1f : Optics.Schlick(cosTheta, etaI, etaT);

        if (sampler.Next1D() < reflectProb)
        {
            wi = Optics.Reflect(incident, hit.Normal).Normalized();
        }
        else
        {
            if (!Optics.Refract(incident, hit.Normal, eta, out wi))
                wi = Optics.Reflect(incident, hit.Normal).Normalized();
        }

        float cos = float.Abs(Vec3.Dot(wi, hit.Normal));
        if (cos <= 0f)
        {
            pdf = 0f;
            f = Vec3.Zero;
            return false;
        }

        // Delta distribution trick: pdf=1, f = 1/|cos| so throughput becomes 1.
        pdf = 1f;
        f = Vec3.One / cos;
        return true;
    }

    /// <summary>
    /// Beer-Lambert transmittance over distance d.
    /// </summary>
    public Vec3 Transmittance(float distance)
    {
        if (SigmaA.NearZero() || distance <= 0f)
            return Vec3.One;

        return new Vec3(
            float.Exp(-SigmaA.X * distance),
            float.Exp(-SigmaA.Y * distance),
            float.Exp(-SigmaA.Z * distance));
    }

    private static Vec3 ComputeSigmaA(in Vec3 tint, float absorptionStrength)
    {
        // Clamp tint to avoid log(0) or negative.
        float ClampTint(float x) => MathUtil.Clamp(x, 1e-6f, 1f);

        float tx = ClampTint(tint.X);
        float ty = ClampTint(tint.Y);
        float tz = ClampTint(tint.Z);

        // sigma = -ln(tint) * strength
        return new Vec3(
            -float.Log(tx) * absorptionStrength,
            -float.Log(ty) * absorptionStrength,
            -float.Log(tz) * absorptionStrength);
    }
}
