using Core.Lights;
using Core.Materials;
using Core.Math;
using Core.Random;
using Core.Rendering;
using Core.Sampling;

namespace Core.PhotonMapping;

public static class PhotonTracer
{
    public static List<Photon> TracePhotonPass(
        Scene.Scene scene,
        int photonsPerPass,
        int maxDepth,
        ulong baseSeed,
        int iterationIndex,
        PhotonTraceStats stats)
    {
        stats.PhotonsRequested = photonsPerPass;

        var emitters = new List<IPhotonEmitter>();
        foreach (var l in scene.Lights)
        {
            if (l is IPhotonEmitter pe)
                emitters.Add(pe);
        }

        if (emitters.Count == 0)
            throw new InvalidOperationException("No photon emitters available in scene.Lights (need RectAreaLightXZ implementing IPhotonEmitter).");

        // Build CDF for selecting lights proportionally to power
        var powers = new Vec3[emitters.Count];
        var cdf = new float[emitters.Count];
        float sum = 0f;

        for (int i = 0; i < emitters.Count; i++)
        {
            powers[i] = emitters[i].Power;
            float p = powers[i].X + powers[i].Y + powers[i].Z; // scalar weight
            sum += p;
            cdf[i] = sum;
        }

        if (sum <= 0f)
            throw new InvalidOperationException("All photon emitters have zero power.");

        // Normalize CDF
        for (int i = 0; i < cdf.Length; i++)
            cdf[i] /= sum;

        var photons = new List<Photon>(photonsPerPass / 2);

        // Deterministic RNG for this photon pass
        var rng = new Pcg32(SeedHash.Hash64(baseSeed, (ulong)iterationIndex));
        var sampler = new Sampler(rng);

        long totalPathLen = 0;

        for (int p = 0; p < photonsPerPass; p++)
        {
            // Choose light
            float u = sampler.Next1D();
            int li = SelectCdf(cdf, u);
            var emitter = emitters[li];

            // Emit photon
            emitter.EmitPhoton(sampler, out var ray, out _);
            stats.PhotonsEmitted++;

            // Compute emitted photon flux = Power / (photonsPerPass * p(select light))
            float pLight = li == 0 ? cdf[0] : (cdf[li] - cdf[li - 1]);
            Vec3 flux = emitter.Power / (photonsPerPass * System.Math.Max(pLight, 1e-8f));

            // Trace photon path
            int len = TracePhotonPath(scene, ray, flux, maxDepth, sampler, photons, stats);
            totalPathLen += len;
        }

        stats.AvgPathLength = photonsPerPass > 0 ? (double)totalPathLen / photonsPerPass : 0.0;

        return photons;
    }

    private static int TracePhotonPath(
        Scene.Scene scene,
        Ray ray,
        Vec3 beta,
        int maxDepth,
        Sampler sampler,
        List<Photon> outPhotons,
        PhotonTraceStats stats)
    {
        int depth = 0;

        while (depth < maxDepth)
        {
            depth++;

            if (!scene.World.Hit(ray, 0.001f, float.PositiveInfinity, out var hit))
                break;

            // Store photon only on Lambertian (your choice #1)
            if (hit.Material is Lambertian && !hit.Material.IsDelta)
            {
                var incoming = (-ray.Direction).Normalized();
                outPhotons.Add(new Photon(hit.Point, incoming, beta, hit.Normal.Normalized()));
                stats.PhotonsStored++;
            }

            // Sample next direction using the material
            Vec3 wo = (-ray.Direction).Normalized();

            if (!hit.Material.Sample(wo, hit, sampler, out var wi, out var pdf, out var f))
                break;

            float cos = Vec3.Dot(wi, hit.Normal);
            float absCos = float.Abs(cos);

            // For non-delta: require hemisphere; for delta allow absCos
            if (!hit.Material.IsDelta)
            {
                if (pdf <= 0f || cos <= 0f) break;
            }
            else
            {
                if (pdf <= 0f || absCos <= 0f) break;
            }

            beta = Vec3.Hadamard(beta, f) * (absCos / pdf);

            // RR after a few bounces (reuse your existing RR heuristic)
            const int rrStart = 3;
            if (depth >= rrStart)
            {
                float cont = RussianRoulette.ContinuationProbability(beta);
                if (sampler.Next1D() > cont)
                {
                    stats.PathsTerminatedRR++;
                    break;
                }
                beta /= cont;
            }

            ray = new Ray(hit.Point, wi, ray.Time);
        }

        if (depth >= maxDepth)
            stats.PathsTerminatedMaxDepth++;

        return depth;
    }

    private static int SelectCdf(float[] cdf, float u)
    {
        int lo = 0, hi = cdf.Length - 1;
        while (lo < hi)
        {
            int mid = (lo + hi) >> 1;
            if (u <= cdf[mid]) hi = mid;
            else lo = mid + 1;
        }
        return lo;
    }
}