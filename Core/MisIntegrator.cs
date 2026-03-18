namespace Core;

/// <summary>
/// Path integrator using Multiple Importance Sampling for direct lighting (§3.7.3).
/// Combines BRDF sampling and explicit light sampling at each diffuse bounce,
/// dramatically reducing noise compared to pure BRDF sampling.
/// </summary>
public sealed class MisIntegrator
{
    /// <summary>Minimum path depth before Russian Roulette termination begins.</summary>
    public int MinDepth { get; init; } = 3;

    /// <summary>Hard maximum path depth.</summary>
    public int MaxDepth { get; init; } = 50;

    /// <summary>Radiance returned when a ray escapes the scene.</summary>
    public Vector3 BackgroundRadiance { get; init; } = Vector3.Zero;

    /// <summary>
    /// Traces a single ray through the scene using MIS for direct lighting.
    /// </summary>
    /// <param name="ray">The primary ray to trace.</param>
    /// <param name="scene">The full scene for shadow ray testing.</param>
    /// <param name="lights">The list of samplable lights in the scene.</param>
    /// <param name="sampler">Per-thread sampler.</param>
    public Vector3 Trace(Ray ray, IHittable scene, IReadOnlyList<ILight> lights,
                         Sampler sampler)
    {
        var radiance = Vector3.Zero;
        var throughput = Vector3.One;

        for (var depth = 0; depth < MaxDepth; depth++)
        {
            if (!scene.Hit(ray, out var hit))
            {
                radiance = radiance + throughput * BackgroundRadiance;
                break;
            }

            // Hit an emissive surface — only count emission on the first bounce
            // or when arriving via a specular path (PDF=1 materials).
            // For diffuse paths, the light was already sampled explicitly.
            if (hit.Material is Materials.Emissive emissive)
            {
                if (depth == 0)
                    radiance = radiance + throughput * emissive.Emit();
                break;
            }

            // Scatter the ray according to the material
            if (!hit.Material.Scatter(ray, hit, sampler,
                    out var attenuation, out var scattered))
                break;

            var brdfPdf = hit.Material.Pdf(ray, hit, scattered);

            // Delta distributions (mirror, glass) — skip MIS light sampling
            var isDelta = brdfPdf >= 1.0;

            if (!isDelta && lights.Count > 0)
            {
                // ── Direct lighting via MIS ───────────────────────────────────
                radiance = radiance + throughput *
                           EstimateDirectLighting(hit, ray, scattered,
                                                  brdfPdf, scene, lights, sampler);
            }

            // ── Continue the path (indirect lighting) ─────────────────────────
            if (brdfPdf <= 0)
                break;

            throughput = throughput * attenuation;

            // §3.6.3 Russian Roulette
            if (depth >= MinDepth)
            {
                var p = Math.Max(throughput.X, Math.Max(throughput.Y, throughput.Z));
                if (sampler.Next() > p) break;
                throughput = throughput / p;
            }

            ray = scattered;
        }

        return radiance;
    }

    /// <summary>
    /// Estimates direct lighting at <paramref name="hit"/> using the
    /// MIS balance heuristic to combine light sampling and BRDF sampling.
    /// </summary>
    private Vector3 EstimateDirectLighting(
        HitRecord hit,
        Ray rayIn,
        Ray brdfScattered,
        double brdfPdf,
        IHittable scene,
        IReadOnlyList<ILight> lights,
        Sampler sampler)
    {
        var direct = Vector3.Zero;

        // Pick one light uniformly at random
        var light = lights[(int)(sampler.Next() * lights.Count) % lights.Count];
        var lightCount = lights.Count;

        // ── Light sample ──────────────────────────────────────────────────────

        var lightPdf = light.Sample(hit.Point, sampler,
            out var pointOnLight, out var lightNormal, out var emission);

        if (lightPdf > 0)
        {
            var toLight = pointOnLight - hit.Point;
            var distToLight = toLight.Length;
            var lightDir = toLight / distToLight;

            // Only light from the front face
            var cosAtLight = Vector3.Dot(lightNormal, -lightDir);
            if (cosAtLight > 0)
            {
                // Cast shadow ray — slightly shorter than full distance to avoid
                // self-intersection with the light surface itself
                var shadowRay = new Ray(hit.Point, lightDir, TMax: distToLight - 1e-4);
                if (!scene.Hit(shadowRay, out _))
                {
                    // Light is visible — evaluate BRDF at light direction
                    var lightScattered = new Ray(hit.Point, lightDir);
                    var brdfAtLight = EvalBrdf(hit.Material, rayIn, hit, lightScattered);
                    var brdfPdfAtLight = hit.Material.Pdf(rayIn, hit, lightScattered);

                    // MIS balance heuristic weight for the light sample
                    var scaledLightPdf = lightPdf * lightCount;
                    var misWeight = scaledLightPdf /
                                        (scaledLightPdf + brdfPdfAtLight);

                    var cosTheta = Math.Max(Vector3.Dot(hit.Normal, lightDir), 0);
                    direct = direct + misWeight * brdfAtLight * emission
                                    * cosTheta / scaledLightPdf;
                }
            }
        }

        // ── BRDF sample ───────────────────────────────────────────────────────

        if (brdfPdf > 0)
        {
            // Check if the BRDF-sampled direction hits a light
            if (scene.Hit(brdfScattered, out var brdfHit) &&
                brdfHit.Material is Materials.Emissive brdfEmissive)
            {
                var brdfEmission = brdfEmissive.Emit();
                var lightPdfAtBrdf = light.Pdf(hit.Point, brdfHit.Point) * lightCount;

                // MIS balance heuristic weight for the BRDF sample
                var misWeight = brdfPdf / (brdfPdf + lightPdfAtBrdf);
                var cosTheta = Math.Max(Vector3.Dot(hit.Normal,
                                    brdfScattered.Direction), 0);

                var brdfVal = EvalBrdf(hit.Material, rayIn, hit, brdfScattered);
                direct = direct + misWeight * brdfVal * brdfEmission
                                * cosTheta / brdfPdf;
            }
        }

        return direct;
    }

    /// <summary>
    /// Evaluates the BRDF value f_r for the given material and directions.
    /// Extracts the per-channel reflectance from the material's scatter weight.
    /// </summary>
    private static Vector3 EvalBrdf(IMaterial material, Ray rayIn,
                                    HitRecord hit, Ray scattered)
    {
        // We recover f_r by calling Scatter with a fixed sampler state.
        // This works because attenuation = f_r·cosθ/pdf for our materials,
        // so f_r = attenuation·pdf/cosθ
        var pdf = material.Pdf(rayIn, hit, scattered);
        if (pdf <= 0) return Vector3.Zero;

        var cosTheta = Math.Max(Vector3.Dot(hit.Normal, scattered.Direction), 1e-10);

        // Create a dummy sampler — Scatter output is deterministic given direction
        // for eval purposes (we only use the attenuation, not the direction)
        var dummySampler = new Sampler(0);
        if (!material.Scatter(rayIn, hit, dummySampler,
                out var attenuation, out _))
            return Vector3.Zero;

        return attenuation * pdf / cosTheta;
    }
}