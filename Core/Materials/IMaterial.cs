using Core.Math;
using Core.Sampling;
using Core.Scene;

namespace Core.Materials;

/// <summary>
/// Material interface for path tracing.
/// 
/// Milestone 3:
/// - Supports emitted radiance (area lights / emissive surfaces)
/// - Supports diffuse scattering (Lambertian)
/// 
/// Later milestones will extend this with specular, transmission, microfacet, PDFs and MIS.
/// </summary>
public interface IMaterial
{
    /// <summary>
    /// Emitted radiance at the hit point.
    /// For non-emissive materials, return Vec3.Zero.
    /// </summary>
    Vec3 Emitted(in Ray rayIn, in HitRecord hit);

    /// <summary>
    /// Samples a scattered ray.
    /// Returns true if scattering occurred; false means the path terminates at this surface.
    /// </summary>
    bool Scatter(in Ray rayIn, in HitRecord hit, Sampler sampler, out Ray scattered, out Vec3 attenuation);
}