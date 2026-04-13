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

    public Vec3 Emitted(in Ray rayIn, in HitRecord hit)
        => hit.FrontFace ? Radiance : Vec3.Zero;

    public bool Scatter(in Ray rayIn, in HitRecord hit, Sampler sampler, out Ray scattered, out Vec3 attenuation)
    {
        scattered = default;
        attenuation = Vec3.Zero;
        return false;
    }
}