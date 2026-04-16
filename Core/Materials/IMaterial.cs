using Core.Math;
using Core.Sampling;
using Core.Scene;

namespace Core.Materials;

/// <summary>
/// Material interface supporting emission and BSDF evaluation/sampling.
/// Designed for Next Event Estimation + MIS.
/// </summary>
public interface IMaterial
{
    /// <summary>
    /// Emitted radiance at the hit point along the outgoing direction.
    /// For non-emissive materials, return Vec3.Zero.
    /// </summary>
    Vec3 Emitted(in Ray rayIn, in HitRecord hit);

    /// <summary>
    /// BSDF evaluation f(wo, wi) (units: 1/sr).
    /// </summary>
    Vec3 Evaluate(in Vec3 wo, in Vec3 wi, in HitRecord hit);

    /// <summary>
    /// BSDF PDF p(wi) over solid angle for the sampling strategy used by Sample.
    /// </summary>
    float Pdf(in Vec3 wo, in Vec3 wi, in HitRecord hit);

    /// <summary>
    /// Samples an incoming direction wi given outgoing wo.
    /// Returns false if the material does not scatter.
    /// </summary>
    bool Sample(in Vec3 wo, in HitRecord hit, Sampler sampler, out Vec3 wi, out float pdf, out Vec3 f);

    /// <summary>
    /// True for delta distributions (perfect specular) so NEE/MIS can skip certain terms.
    /// </summary>
    bool IsDelta { get; }
}
