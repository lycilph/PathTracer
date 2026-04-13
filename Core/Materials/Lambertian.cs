using Core.Math;
using Core.Sampling;

namespace Core.Materials;

/// <summary>
/// Lambertian diffuse BSDF.
/// f = albedo / pi
/// Sampled with cosine-weighted hemisphere.
/// </summary>
public sealed class Lambertian
{
    public Vec3 Albedo { get; }

    public Lambertian(in Vec3 albedo) => Albedo = albedo;

    public Vec3 Sample(in Vec3 normal, Sampler sampler, out Vec3 wi, out float pdf)
    {
        wi = CosineSampleHemisphere(sampler);
        wi = ToWorld(wi, normal);
        pdf = Pdf(wi, normal);
        return Albedo * MathUtil.InvPi;
    }

    public static float Pdf(in Vec3 wi, in Vec3 n)
    {
        float cos = Vec3.Dot(wi.Normalized(), n);
        return cos > 0f ? cos * MathUtil.InvPi : 0f;
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