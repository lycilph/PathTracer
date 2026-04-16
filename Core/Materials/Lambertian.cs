using Core.Math;
using Core.Sampling;
using Core.Scene;

namespace Core.Materials;

/// <summary>
/// Lambertian diffuse material.
/// 
/// BSDF: f = albedo / pi
/// Sample with cosine-weighted hemisphere.
/// Under cosine-weighted sampling, (f * cosθ) / pdf = albedo.
/// So throughput update becomes multiplication by albedo (component-wise for RGB).
/// </summary>
public sealed class Lambertian : IMaterial
{
    public Vec3 Albedo { get; }

    public Lambertian(in Vec3 albedo) => Albedo = albedo;

    public bool IsDelta => false;

    public Vec3 Emitted(in Ray rayIn, in HitRecord hit) => Vec3.Zero;

    public Vec3 Evaluate(in Vec3 wo, in Vec3 wi, in HitRecord hit)
    {
        float cos = Vec3.Dot(wi.Normalized(), hit.Normal);
        if (cos <= 0f) return Vec3.Zero;
        return Albedo * MathUtil.InvPi;
    }

    public float Pdf(in Vec3 wo, in Vec3 wi, in HitRecord hit)
    {
        float cos = Vec3.Dot(wi.Normalized(), hit.Normal);
        return cos > 0f ? cos * MathUtil.InvPi : 0f;
    }

    public bool Sample(in Vec3 wo, in HitRecord hit, Sampler sampler, out Vec3 wi, out float pdf, out Vec3 f)
    {
        Vec3 local = CosineSampleHemisphere(sampler);
        wi = ToWorld(local, hit.Normal);
        pdf = Pdf(wo, wi, hit);
        f = Albedo * MathUtil.InvPi;
        return pdf > 0f;
    }

    private static Vec3 CosineSampleHemisphere(Sampler s)
    {
        float u1 = s.Next1D();
        float u2 = s.Next1D();
        float r = float.Sqrt(u1);
        float theta = MathUtil.TwoPi * u2;
        float x = r * float.Cos(theta);
        float y = r * float.Sin(theta);
        float z = float.Sqrt(1f - u1);
        return new Vec3(x, y, z);
    }

    private static Vec3 ToWorld(in Vec3 local, in Vec3 n)
    {
        Vec3 w = n;
        Vec3 a = float.Abs(w.X) > 0.9f ? Vec3.UnitY : Vec3.UnitX;
        Vec3 v = Vec3.Cross(w, a).Normalized();
        Vec3 u = Vec3.Cross(v, w);
        return (u * local.X + v * local.Y + w * local.Z).Normalized();
    }
}
