using Core.Math;
using Core.Sampling;
using Core.Scene;

namespace Core.Materials;


// <summary>
/// Perfect specular reflection (delta BSDF).
///
/// We implement sampling only. Evaluate/Pdf return 0 because the BSDF is a delta distribution.
///
/// To keep the integrator throughput update compatible with the existing
/// throughput = f * |cos| / pdf form, we return:
///   pdf = 1
///   f = albedo / |cos|
/// so throughput becomes albedo.
/// </summary>
public sealed class Mirror : IMaterial
{
    public Vec3 Albedo { get; }

    public Mirror(in Vec3 albedo) => Albedo = albedo;

    public bool IsDelta => true;

    public Vec3 Emitted(in Ray rayIn, in HitRecord hit) => Vec3.Zero;

    public Vec3 Evaluate(in Vec3 wo, in Vec3 wi, in HitRecord hit) => Vec3.Zero;

    public float Pdf(in Vec3 wo, in Vec3 wi, in HitRecord hit) => 0f;

    public bool Sample(in Vec3 wo, in HitRecord hit, Sampler sampler, out Vec3 wi, out float pdf, out Vec3 f)
    {
        // Incident direction is -wo (points into surface)
        Vec3 incident = (-wo).Normalized();
        wi = Optics.Reflect(incident, hit.Normal).Normalized();

        float cos = float.Abs(Vec3.Dot(wi, hit.Normal));
        if (cos <= 0f)
        {
            pdf = 0f;
            f = Vec3.Zero;
            return false;
        }

        pdf = 1f;
        f = Albedo / cos;
        return true;
    }
}