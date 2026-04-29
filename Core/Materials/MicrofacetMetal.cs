using Core.Math;
using Core.Sampling;
using Core.Scene;

namespace Core.Materials;

/// <summary>
/// GGX microfacet metal (reflection only).
///
/// Uses Cook-Torrance BRDF:
/// f = (D * G * F) / (4 * cosI * cosO)
///
/// F is Schlick Fresnel with RGB F0 (metal reflectance at normal incidence).
///
/// Roughness is perceptual in [0,1]; alpha = roughness^2 with a small clamp.
/// </summary>
public sealed class MicrofacetMetal : IMaterial
{
    public Vec3 F0 { get; }
    public float Roughness { get; }

    public bool IsDelta => false;

    public MicrofacetMetal(in Vec3 f0, float roughness)
    {
        F0 = f0;
        Roughness = MathUtil.Clamp(roughness, 0.001f, 1f);
    }

    public Vec3 Emitted(in Ray rayIn, in HitRecord hit) => Vec3.Zero;

    private float Alpha => Roughness * Roughness;

    public Vec3 Evaluate(in Vec3 wo, in Vec3 wi, in HitRecord hit)
    {
        float cosO = Vec3.Dot(wo, hit.Normal);
        float cosI = Vec3.Dot(wi, hit.Normal);
        if (cosO <= 0f || cosI <= 0f) return Vec3.Zero;

        Vec3 h = (wi + wo);
        if (h.NearZero()) return Vec3.Zero;
        h = h.Normalized();

        float cosH = Vec3.Dot(h, hit.Normal);
        if (cosH <= 0f) return Vec3.Zero;

        float D = Ggx.D_Ggx(Alpha, cosH);
        float G = Ggx.G_Smith(Alpha, cosI, cosO);

        float cosWiH = float.Max(0f, Vec3.Dot(wi, h));
        Vec3 F = Ggx.FresnelSchlick(F0, cosWiH);

        float denom = 4f * cosI * cosO;
        float scalar = (D * G) / denom;

        return F * scalar;
    }

    public float Pdf(in Vec3 wo, in Vec3 wi, in HitRecord hit)
    {
        float cosO = Vec3.Dot(wo, hit.Normal);
        float cosI = Vec3.Dot(wi, hit.Normal);
        if (cosO <= 0f || cosI <= 0f) return 0f;

        Vec3 h = (wi + wo);
        if (h.NearZero()) return 0f;
        h = h.Normalized();

        float cosH = Vec3.Dot(h, hit.Normal);
        if (cosH <= 0f) return 0f;

        float D = Ggx.D_Ggx(Alpha, cosH);

        // pdf_h = D(h) * cosH
        float pdfH = D * cosH;

        float woDotH = Vec3.Dot(wo, h);
        if (woDotH <= 0f) return 0f;

        // Convert half-vector PDF to direction PDF:
        // pdf(wi) = pdf(h) / (4 * dot(wo, h))
        return pdfH / (4f * woDotH);
    }


    public bool Sample(in Vec3 wo, in HitRecord hit, Sampler sampler, out Vec3 wi, out float pdf, out Vec3 f)
    {
        float cosO = Vec3.Dot(wo, hit.Normal);
        if (cosO <= 0f)
        {
            wi = default;
            pdf = 0f;
            f = Vec3.Zero;
            return false;
        }

        // Rejection sampling: NDF sampling can produce wi below the surface.
        // Try a few times; if all fail, return false.
        const int maxTries = 16;

        for (int attempt = 0; attempt < maxTries; attempt++)
        {
            float u1 = sampler.Next1D();
            float u2 = sampler.Next1D();

            Vec3 hLocal = Ggx.SampleHalfVector(Alpha, u1, u2);
            Vec3 h = Ggx.ToWorld(hLocal, hit.Normal);

            // Reflect -wo about h to get wi
            Vec3 incident = (-wo).Normalized();
            wi = Optics.Reflect(incident, h).Normalized();

            float cosI = Vec3.Dot(wi, hit.Normal);
            if (cosI <= 0f)
                continue;

            // Extra guard: wo·h must be > 0 for pdf conversion
            float woDotH = Vec3.Dot(wo, h);
            if (woDotH <= 0f)
                continue;

            pdf = Pdf(wo, wi, hit);
            if (pdf <= 0f)
                continue;

            f = Evaluate(wo, wi, hit);
            if (f.NearZero())
                continue;

            return true;
        }

        wi = default;
        pdf = 0f;
        f = Vec3.Zero;
        return false;
    }
}