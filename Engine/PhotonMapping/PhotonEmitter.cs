using Core;
using Core.Algebra;
using Core.Geometry;
using Core.Sampling;
using Engine.Materials;

namespace Engine.PhotonMapping;

/// <summary>
/// Traces photons forward from light sources into the scene, storing
/// them at diffuse surface interactions in a photon map.
/// Emission is parallelized across photons; kd-tree construction is
/// handled by the caller after all photons are collected.
/// </summary>
public sealed class PhotonEmitter
{
    /// <summary>
    /// Emits <paramref name="photonCount"/> photons from all lights
    /// into the scene and returns the collected photons.
    /// </summary>
    /// <param name="photonCount">
    /// Total number of photons to emit across all lights.
    /// </param>
    /// <param name="scene">The scene to trace photons through.</param>
    /// <param name="lights">The lights to emit photons from.</param>
    /// <param name="maxDepth">
    /// Maximum number of bounces per photon path. Default 20.
    /// </param>
    /// <param name="cancellationToken">Token to cancel emission.</param>
    /// <returns>
    /// All photons deposited at diffuse surfaces. Direct photons
    /// (first diffuse hit with no prior specular bounces) are excluded
    /// since direct lighting is handled by MIS.
    /// </returns>
    public List<Photon> Emit(
        int photonCount,
        IHittable scene,
        IReadOnlyList<ILight> lights,
        int maxDepth = 20,
        CancellationToken cancellationToken = default)
    {
        if (lights.Count == 0) return [];

        // Thread-safe collection for parallel emission
        var photons = new System.Collections.Concurrent.ConcurrentBag<Photon>();

        // Distribute photons evenly across lights
        var photonsPerLight = photonCount / lights.Count;

        // Total power across all lights for normalization
        var totalPower = ComputeTotalPower(lights);

        try
        {
            Parallel.For(0, photonCount,
            new ParallelOptions { CancellationToken = cancellationToken },
            i =>
            {
                if (cancellationToken.IsCancellationRequested) return;

                // Each photon gets its own sampler seeded by index
                var sampler = new Sampler(HashSeed(i));

                // Pick a light weighted by power
                var (light, lightPower) = PickLight(lights, totalPower, sampler);

                // Emit one photon from the chosen light
                TracePhoton(light, lightPower, photonCount,
                            scene, sampler, maxDepth, photons);
            });
        }
        catch (OperationCanceledException)
        {
            // Return partial results collected so far
        }

        return photons.ToList();
    }

    // ── Photon tracing ────────────────────────────────────────────────────────

    private static void TracePhoton(
        ILight light,
        Vector3 lightPower,
        int totalPhotonCount,
        IHittable scene,
        Sampler sampler,
        int maxDepth,
        System.Collections.Concurrent.ConcurrentBag<Photon> photons)
    {
        // Sample a point and direction from the light
        var (origin, direction, power) = SampleLightRay(
            light, lightPower, totalPhotonCount, sampler);

        var ray = new Ray(origin, direction);
        var hasHadSpecularBounce = false;
        var hasHadDiffuseBounce = false;

        for (var depth = 0; depth < maxDepth; depth++)
        {
            if (!scene.Hit(ray, out var hit)) break;

            var material = hit.Material;

            // If we hit an emissive surface (the light itself on first bounce,
            // or another light later) — skip it by nudging forward rather
            // than terminating, but only on the first bounce.
            // On subsequent bounces terminate to avoid infinite loops.
            if (material is Emissive)
            {
                if (depth == 0)
                {
                    // First hit is the light surface itself — skip it
                    ray = new Core.Algebra.Ray(hit.Point, ray.Direction);
                    continue;
                }
                break;
            }

            var isDiffuse = material is Lambertian;
            var isSpecular = material is Mirror or Dielectric or GgxMetal;

            if (isDiffuse)
            {
                // Determine photon path type
                var pathType = DeterminePathType(
                    hasHadSpecularBounce, hasHadDiffuseBounce);

                // Store photon — but not direct photons (handled by MIS)
                if (pathType != PhotonPathType.Direct)
                {
                    photons.Add(new Photon(
                        hit.Point,
                        ray.Direction,
                        power,
                        pathType));
                }

                // Russian Roulette based on surface albedo, not power magnitude
                // This is independent of photon count normalization
                var albedo = material is Lambertian l ? l.Albedo : Vector3.One;
                var survivalProb = Math.Min(
                    Math.Max(albedo.X, Math.Max(albedo.Y, albedo.Z)), 0.95);

                if (sampler.Next() > survivalProb) break;
                power = power / survivalProb;

                hasHadDiffuseBounce = true;

                // Scatter diffusely using cosine-weighted sampling
                var scatterDir = sampler.CosineWeightedHemisphere(hit.Normal);
                ray = new Ray(hit.Point, scatterDir);

                // Attenuate power by BRDF
                if (material is Lambertian lambertian)
                    power = power * lambertian.Albedo;
            }
            else if (isSpecular)
            {
                hasHadSpecularBounce = true;

                // Scatter specularly
                if (!material.Scatter(ray, hit, sampler,
                        out var attenuation, out var scattered))
                    break;

                power = power * attenuation;
                ray = scattered;
            }
            else
            {
                break;
            }

            // Terminate if power is negligible
            if (power.X < 1e-6 && power.Y < 1e-6 && power.Z < 1e-6)
                break;
        }
    }

    // ── Light sampling ────────────────────────────────────────────────────────

    private static (Vector3 origin, Vector3 direction, Vector3 power)
        SampleLightRay(ILight light, Vector3 lightPower,
                       int totalPhotonCount, Sampler sampler)
    {
        // Sample a point on the light surface
        light.Sample(Vector3.Zero, sampler,
            out var pointOnLight, out var lightNormal, out _);

        // Sample a direction from the light using cosine-weighted emission
        var direction = sampler.CosineWeightedHemisphere(lightNormal);

        // Scale power by number of photons for correct flux normalization
        var power = lightPower / totalPhotonCount;

        return (pointOnLight, direction, power);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static PhotonPathType DeterminePathType(
        bool hasHadSpecularBounce, bool hasHadDiffuseBounce)
    {
        if (!hasHadSpecularBounce && !hasHadDiffuseBounce)
            return PhotonPathType.Direct;
        if (hasHadSpecularBounce && !hasHadDiffuseBounce)
            return PhotonPathType.Caustic;
        return PhotonPathType.Indirect;
    }

    private static Vector3 ComputeTotalPower(IReadOnlyList<ILight> lights)
    {
        var total = Vector3.Zero;
        foreach (var light in lights)
        {
            // Sample the light to get its emission
            var sampler = new Sampler(0);
            light.Sample(Vector3.Zero, sampler,
                out _, out _, out var emission);
            total = total + emission;
        }
        return total;
    }

    private static (ILight light, Vector3 power) PickLight(
        IReadOnlyList<ILight> lights,
        Vector3 totalPower,
        Sampler sampler)
    {
        if (lights.Count == 1)
        {
            var s = new Sampler(0);
            lights[0].Sample(Vector3.Zero, s, out _, out _, out var e);
            return (lights[0], e);
        }

        // Pick light proportional to its power
        var target = sampler.Next()
                   * (totalPower.X + totalPower.Y + totalPower.Z) / 3.0;

        var cumulative = 0.0;
        foreach (var light in lights)
        {
            var s = new Sampler(0);
            light.Sample(Vector3.Zero, s, out _, out _, out var emission);
            var power = (emission.X + emission.Y + emission.Z) / 3.0;
            cumulative += power;
            if (cumulative >= target)
                return (light, emission);
        }

        // Fallback — return last light
        var fallbackSampler = new Sampler(0);
        lights[^1].Sample(Vector3.Zero, fallbackSampler,
            out _, out _, out var fallbackEmission);
        return (lights[^1], fallbackEmission);
    }

    private static int HashSeed(int i)
    {
        unchecked
        {
            var hash = (int)2166136261;
            hash = (hash ^ i) * 16777619;
            return hash;
        }
    }
}