using Core.Math;
using Core.Sampling;
using Core.Scene;

namespace Core.Materials;

/// <summary>
/// Diffuse area light / emissive material.
/// Emits constant radiance. Does not scatter.
/// </summary>
public sealed class DiffuseLight : IMaterial
{
    public Vec3 Radiance { get; }

    public DiffuseLight(in Vec3 radiance) => Radiance = radiance;

    public bool IsDelta => true; // it doesn't scatter; treat as delta-ish for MIS decisions

    public Vec3 Emitted(in Ray rayIn, in HitRecord hit)
        => hit.FrontFace ? Radiance : Vec3.Zero;

    public Vec3 Evaluate(in Vec3 wo, in Vec3 wi, in HitRecord hit) => Vec3.Zero;

    public float Pdf(in Vec3 wo, in Vec3 wi, in HitRecord hit) => 0f;

    public bool Sample(in Vec3 wo, in HitRecord hit, Sampler sampler, out Vec3 wi, out float pdf, out Vec3 f)
    {
        wi = default;
        pdf = 0f;
        f = Vec3.Zero;
        return false;
    }
}
