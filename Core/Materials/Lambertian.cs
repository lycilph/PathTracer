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

    public Vec3 Emitted(in Ray rayIn, in HitRecord hit) => Vec3.Zero;

    public bool Scatter(in Ray rayIn, in HitRecord hit, Sampler sampler, out Ray scattered, out Vec3 attenuation)
    {
        Vec3 wiLocal = CosineSampleHemisphere(sampler);
        Vec3 wi = ToWorld(wiLocal, hit.Normal);

        scattered = new Ray(hit.Point, wi, rayIn.Time);
        attenuation = Albedo;
        return true;
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
        // Orthonormal basis from n
        Vec3 w = n;
        Vec3 a = float.Abs(w.X) > 0.9f ? Vec3.UnitY : Vec3.UnitX;
        Vec3 v = Vec3.Cross(w, a).Normalized();
        Vec3 u = Vec3.Cross(v, w);

        return (u * local.X + v * local.Y + w * local.Z).Normalized();
    }
}