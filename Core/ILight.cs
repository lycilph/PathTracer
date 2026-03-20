using Core.Algebra;
using Core.Sampling;

namespace Core;

/// <summary>
/// Implemented by any light source that supports direct sampling for MIS (§3.7.3).
/// </summary>
public interface ILight
{
    /// <summary>
    /// Samples a point on the light surface visible from <paramref name="origin"/>.
    /// </summary>
    /// <param name="origin">The surface point we are lighting.</param>
    /// <param name="sampler">Per-thread sampler for stochastic sampling.</param>
    /// <param name="pointOnLight">Output: the sampled point on the light.</param>
    /// <param name="normal">Output: the surface normal at the sampled point.</param>
    /// <param name="emission">Output: the emitted radiance at the sampled point.</param>
    /// <returns>
    /// The PDF of having sampled this point, in solid angle measure at
    /// <paramref name="origin"/>. Returns 0 if the light is not visible.
    /// </returns>
    double Sample(Vector3 origin, Sampler sampler,
                  out Vector3 pointOnLight,
                  out Vector3 normal,
                  out Vector3 emission);

    /// <summary>
    /// Returns the PDF (solid angle measure) of having sampled the direction
    /// toward <paramref name="pointOnLight"/> from <paramref name="origin"/>.
    /// Used to compute the MIS weight for light samples.
    /// </summary>
    double Pdf(Vector3 origin, Vector3 pointOnLight);
}