using Core.Math;
using Core.Sampling;
using Core.Scene;

namespace Core.Materials;

/// <summary>
/// Ideal dielectric (glass) with Fresnel reflection + refraction (delta BSDF).
///
/// Uses Schlick approximation for Fresnel reflectance.
/// Handles total internal reflection (TIR).
///
/// Similar to Mirror, we return:
///   pdf = 1
///   f = attenuation / |cos|
/// so throughput becomes attenuation (Vec3.One for clear glass).
/// </summary>
public sealed class Dielectric : IMaterial
{
    public float Ior { get; } // index of refraction

    public Dielectric(float ior = 1.5f) => Ior = ior;

    public bool IsDelta => true;

    public Vec3 Emitted(in Ray rayIn, in HitRecord hit) => Vec3.Zero;

    public Vec3 Evaluate(in Vec3 wo, in Vec3 wi, in HitRecord hit) => Vec3.Zero;

    public float Pdf(in Vec3 wo, in Vec3 wi, in HitRecord hit) => 0f;

    public bool Sample(in Vec3 wo, in HitRecord hit, Sampler sampler, out Vec3 wi, out float pdf, out Vec3 f)
    {
        Vec3 incident = (-wo).Normalized();

        float etaI = 1f;
        float etaT = Ior;

        // hit.Normal is oriented against incident for front faces (HitRecord).
        // If we're exiting, swap indices.
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

        pdf = 1f;
        f = Vec3.One / cos; // attenuation = 1 for clear glass (no absorption)
        return true;
    }
}