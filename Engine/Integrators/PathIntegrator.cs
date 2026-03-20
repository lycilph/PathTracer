using Core.Algebra;
using Core.Geometry;
using Core.Sampling;

namespace Engine.Rendering;

/// <summary>
/// Unidirectional path tracer implementing the rendering equation
/// via Monte Carlo integration (§3.6).
/// </summary>
public sealed class PathIntegrator
{
    /// <summary>Minimum path depth before Russian Roulette termination begins.</summary>
    public int MinDepth { get; init; } = 3;

    /// <summary>Hard maximum path depth — paths are always terminated here.</summary>
    public int MaxDepth { get; init; } = 50;

    /// <summary>Radiance returned when a ray escapes the scene.</summary>
    public Vector3 BackgroundRadiance { get; init; } = Vector3.Zero;

    /// <summary>
    /// Traces a single ray through the scene and returns the estimated radiance.
    /// </summary>
    /// <param name="ray">The primary ray to trace.</param>
    /// <param name="scene">The scene to trace against.</param>
    /// <param name="sampler">Per-thread sampler for stochastic decisions.</param>
    /// <returns>
    /// Estimated incoming radiance along the ray direction. HDR — not tone-mapped.
    /// </returns>
    public Vector3 Trace(Ray ray, IHittable scene, Sampler sampler)
    {
        var radiance = Vector3.Zero;
        var throughput = Vector3.One;

        for (var depth = 0; depth < MaxDepth; depth++)
        {
            if (!scene.Hit(ray, out var hit))
            {
                // Ray escaped — add background (sky, black void, etc.)
                radiance = radiance + throughput * BackgroundRadiance;
                break;
            }

            // Hit an emissive surface — collect light and end path
            if (hit.Material is Materials.Emissive emissive)
            {
                radiance = radiance + throughput * emissive.Emit();
                break;
            }

            // Ask the material how to scatter
            if (!hit.Material.Scatter(ray, hit, sampler,
                    out var attenuation, out var scattered))
            {
                // Material absorbed the ray entirely
                break;
            }

            throughput = throughput * attenuation;

            // §3.6.3 Russian Roulette — only after minimum depth
            if (depth >= MinDepth)
            {
                var p = Math.Max(throughput.X, Math.Max(throughput.Y, throughput.Z));
                if (sampler.Next() > p)
                    break;

                // Boost surviving paths to maintain unbiased estimator
                throughput = throughput / p;
            }

            ray = scattered;
        }

        return radiance;
    }
}