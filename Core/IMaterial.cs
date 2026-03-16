namespace Core;

/// <summary>
/// Determines how a surface interacts with light (§3.8).
/// A material either scatters the incoming ray or absorbs it entirely.
/// </summary>
public interface IMaterial
{
    /// <summary>
    /// Computes the scattered ray and energy attenuation for an incoming ray.
    /// </summary>
    /// <param name="rayIn">The incoming ray that struck the surface.</param>
    /// <param name="hit">The hit record describing the intersection.</param>
    /// <param name="sampler">Per-thread random sampler for stochastic scattering.</param>
    /// <param name="attenuation">
    /// Output: the fraction of light energy carried forward. RGB in [0,1].
    /// </param>
    /// <param name="scattered">Output: the new ray to trace if scattering occurred.</param>
    /// <returns>True if the ray scattered; false if it was fully absorbed.</returns>
    bool Scatter(Ray rayIn, HitRecord hit, Sampler sampler,
                 out Vector3 attenuation, out Ray scattered);
}