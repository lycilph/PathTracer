using System.Collections.Concurrent;
using System.Diagnostics;
using Core.Camera;
using Core.Lights;
using Core.Materials;
using Core.Math;
using Core.Random;
using Core.Sampling;
using Core.Scene;

namespace Core.Rendering.Sppm;

/// <summary>
/// Stochastic Progressive Photon Mapping renderer.
///
/// Each iteration consists of four phases:
///   1. Camera pass   – trace eye rays; record the first diffuse hit point per pixel.
///   2. Photon pass   – emit M photons from lights; store every diffuse bounce.
///   3. Gather        – for each hit point, sum photon flux within its search radius.
///   4. Update        – apply the SPPM progressive formula to shrink radii and
///                      accumulate the flux estimate τ.
///
/// Final pixel colour = τ / (π R² × total_photons_emitted)  +  avg_direct_light.
///
/// Reference: Hachisuka et al., "Stochastic Progressive Photon Mapping", SIGGRAPH Asia 2009.
/// </summary>
public static class SppmRenderer
{
    // Alpha controls the rate of radius reduction.  0.7 is the canonical value.
    public const float DefaultAlpha               = 0.7f;
    public const int   DefaultPhotonsPerIteration = 200_000;
    public const float DefaultInitialRadius       = 10f;
    private const int  MaxDepth                   = 20;
    private const float RussianRouletteThreshold  = 0.95f;

    // ── Public entry point ────────────────────────────────────────────────────

    /// <summary>
    /// Runs the SPPM render loop until the cancellation token fires or
    /// <paramref name="maxIterations"/> is reached.
    ///
    /// <paramref name="pixelStates"/> must be pre-allocated with width×height elements
    /// and initialised via <see cref="CreatePixelStates"/>.
    ///
    /// <paramref name="reportFrame"/> is called after every iteration with a
    /// linear-light Vec3[] (length = width × height, row-major) ready for gamma-encode
    /// and display.
    /// </summary>
    public static async Task RenderLoopAsync(
        int             width,
        int             height,
        ICamera         camera,
        Scene.Scene     scene,
        SppmPixelState[] pixelStates,
        int             photonsPerIteration,
        float           alpha,
        CancellationToken            token,
        Action<SppmProgress>?        reportProgress,
        Action<Vec3[]>?              reportFrame,
        int?                         maxDegreeOfParallelism = null,
        int?                         maxIterations          = null)
    {
        var sw = Stopwatch.StartNew();

        var parallelOpts = new ParallelOptions
        {
            CancellationToken      = token,
            MaxDegreeOfParallelism = maxDegreeOfParallelism ?? Environment.ProcessorCount
        };

        // Thread-local scratch buffers for photon gather queries
        var tlGatherList = new ThreadLocal<List<int>>(() => new List<int>());

        int  iteration          = 0;
        long totalPhotonsEmitted = 0;

        var grid = new PhotonHashGrid();

        while (!token.IsCancellationRequested)
        {
            if (maxIterations.HasValue && iteration >= maxIterations.Value) break;
            iteration++;

            // ── Phase 1: Camera pass ───────────────────────────────────────────
            RunCameraPass(width, height, camera, scene, pixelStates, iteration, parallelOpts);

            // ── Phase 2: Photon pass ───────────────────────────────────────────
            StoredPhoton[] photons = TracePhotons(scene, photonsPerIteration, iteration, parallelOpts, token);
            if (token.IsCancellationRequested) break;
            totalPhotonsEmitted += photonsPerIteration;

            // ── Phase 3: Build hash grid ───────────────────────────────────────
            float maxR = MaxCurrentRadius(pixelStates, width * height);
            grid.Build(photons, maxR);

            // ── Phase 4: Gather photons at each hit point ──────────────────────
            GatherPhotons(pixelStates, width * height, grid, scene, parallelOpts, tlGatherList);

            // ── Phase 5: Progressive update ────────────────────────────────────
            UpdateStates(pixelStates, width * height, alpha, parallelOpts);

            // ── Phase 6: Reconstruct image ─────────────────────────────────────
            Vec3[] frame = ReconstructFrame(pixelStates, width, height, totalPhotonsEmitted);

            reportFrame?.Invoke(frame);

            reportProgress?.Invoke(new SppmProgress(
                Iteration:             iteration,
                AverageRadius:         AverageRadius(pixelStates, width * height),
                TotalPhotonsEmitted:   totalPhotonsEmitted,
                PhotonsPerIteration:   photonsPerIteration,
                ElapsedSeconds:        sw.Elapsed.TotalSeconds,
                IterationsPerSecond:   iteration / sw.Elapsed.TotalSeconds));

            await Task.Yield(); // yield to allow UI updates / cancellation checks
        }
    }

    /// <summary>Allocates and initialises per-pixel SPPM state with the given initial radius.</summary>
    public static SppmPixelState[] CreatePixelStates(int width, int height, float initialRadius)
    {
        int n = width * height;
        var states = new SppmPixelState[n];
        for (int i = 0; i < n; i++)
            states[i] = new SppmPixelState { Radius = initialRadius };
        return states;
    }

    // ── Phase 1: Camera pass ──────────────────────────────────────────────────

    private static void RunCameraPass(
        int width, int height,
        ICamera camera, Scene.Scene scene,
        SppmPixelState[] states,
        int iteration,
        ParallelOptions opts)
    {
        Parallel.For(0, width * height, opts, pixelIndex =>
        {
            int px = pixelIndex % width;
            int py = pixelIndex / width;

            // Unique, deterministic seed per (pixel, iteration)
            ulong seed = SeedHash.PixelSampleSeed(px, py, (iteration * 1_000_003), 0xABCD_EF01u);
            var rng     = new Pcg32(seed, 0);
            var sampler = new Sampler(rng);

            var state = states[pixelIndex];
            state.IsValid     = false;
            state.NewPhotons  = 0;
            state.NewFlux     = Vec3.Zero;
            state.DirectLight = Vec3.Zero;

            // Sub-pixel jitter
            float u = (px + sampler.Next1D()) / width;
            float v = (py + sampler.Next1D()) / height;

            var ray = camera.GetRay(u, 1f - v, sampler);
            TraceCameraRay(ray, scene, sampler, state);
        });
    }

    /// <summary>
    /// Traces a single camera ray.  Follows delta (specular/glass) surfaces until
    /// hitting a non-delta surface, where the hit point is recorded.
    /// Direct illumination (emission + NEE) is accumulated along the way.
    /// </summary>
    private static void TraceCameraRay(
        Ray ray, Scene.Scene scene, Sampler sampler, SppmPixelState state)
    {
        Vec3 throughput    = Vec3.One;
        Vec3 directLight   = Vec3.Zero;
        Vec3 mediumSigmaA  = Vec3.Zero;      // Beer-Lambert absorption in current medium

        for (int depth = 0; depth < MaxDepth; depth++)
        {
            if (!scene.World.Hit(ray, 1e-4f, float.PositiveInfinity, out var hit))
                break;

            // Beer-Lambert transmittance through the medium
            Vec3 transmittance = MediumTransmittance(mediumSigmaA, hit.T);
            Vec3 effThroughput  = Vec3.Hadamard(throughput, transmittance);

            var  mat = hit.Material;
            Vec3 wo  = (-ray.Direction).Normalized();

            // Always collect emission (e.g. camera ray directly hitting a light)
            Vec3 Le = mat.Emitted(ray, hit);
            if (!Le.NearZero())
                directLight += Vec3.Hadamard(effThroughput, Le);

            if (!mat.IsDelta)
            {
                // ── Non-delta (diffuse / rough metal) ── record hit point ──────
                Vec3 direct = EstimateDirect(hit, wo, scene, sampler, ray.Time);
                directLight += Vec3.Hadamard(effThroughput, direct);

                state.IsValid                = true;
                state.Position               = hit.Point;
                state.Normal                 = hit.Normal;
                state.Wo                     = wo;
                state.Hit                    = hit;
                state.CameraPathThroughput   = effThroughput;
                state.DirectLight            = directLight;
                return;
            }

            // ── Delta surface ── follow the specular bounce ───────────────────
            if (!mat.Sample(wo, hit, sampler, out Vec3 wi, out float pdf, out Vec3 f))
                break;

            float absCos = float.Abs(Vec3.Dot(wi, hit.Normal));
            if (pdf <= 0f || absCos == 0f) break;

            throughput = Vec3.Hadamard(effThroughput, f * (absCos / pdf));

            // Track medium transition (entering / exiting dielectric)
            mediumSigmaA = MediumAfterBounce(mat, wi, hit, mediumSigmaA);

            ray = new Ray(hit.Point, wi, ray.Time);
        }

        // No non-delta hit found – store direct-light-only pixel
        state.IsValid     = false;
        state.DirectLight = directLight;
    }

    // ── Phase 2: Photon pass ──────────────────────────────────────────────────

    private static StoredPhoton[] TracePhotons(
        Scene.Scene scene, int M, int iteration,
        ParallelOptions opts, CancellationToken token)
    {
        var emitters = scene.Lights
            .OfType<IPhotonEmitter>()
            .ToArray();

        if (emitters.Length == 0) return [];

        // Collect photons from all threads into a concurrent bag, then flatten.
        var bag = new ConcurrentBag<StoredPhoton[]>();

        Parallel.For(0, M, opts,
            // Thread-local list avoids contention
            () => new List<StoredPhoton>(64),
            (i, _, localList) =>
            {
                if (token.IsCancellationRequested) return localList;

                ulong seed = SeedHash.PixelSampleSeed(i, iteration, 123, 0x1234_5678u);
                var rng     = new Pcg32(seed, 1);
                var sampler = new Sampler(rng);

                // Choose a light uniformly (scale power by emitter count)
                int eIdx    = (int)(sampler.Next1D() * emitters.Length);
                if (eIdx >= emitters.Length) eIdx = emitters.Length - 1;

                var emission = emitters[eIdx].EmitPhoton(sampler);
                Vec3 power   = emission.Power * emitters.Length; // unbiased uniform selection

                TracePhoton(scene, emission.Position, emission.Direction, power, sampler, localList);
                return localList;
            },
            localList => bag.Add(localList.ToArray()));

        // Flatten
        int total = 0;
        foreach (var arr in bag) total += arr.Length;
        var result = new StoredPhoton[total];
        int offset = 0;
        foreach (var arr in bag) { arr.CopyTo(result, offset); offset += arr.Length; }
        return result;
    }

    /// <summary>
    /// Traces one photon through the scene.  Stores it in <paramref name="list"/> at
    /// every non-delta (diffuse) surface it hits.  Follows specular/glass via BSDF
    /// sampling.  Terminates via Russian roulette.
    /// </summary>
    private static void TracePhoton(
        Scene.Scene scene,
        Vec3 pos, Vec3 dir, Vec3 power,
        Sampler sampler,
        List<StoredPhoton> list)
    {
        var ray        = new Ray(pos, dir);
        Vec3 throughput = power;
        Vec3 mediumSA   = Vec3.Zero;

        for (int depth = 0; depth < MaxDepth; depth++)
        {
            if (!scene.World.Hit(ray, 1e-4f, float.PositiveInfinity, out var hit))
                break;

            Vec3 trans = MediumTransmittance(mediumSA, hit.T);
            throughput  = Vec3.Hadamard(throughput, trans);

            var  mat = hit.Material;
            Vec3 wi  = ray.Direction.Normalized(); // photon travel direction (toward surface)
            Vec3 wo  = -wi;                        // outward from surface

            if (!mat.IsDelta)
            {
                // Store the photon at this diffuse bounce (needed for caustics + indirect)
                list.Add(new StoredPhoton(hit.Point, wi, throughput));

                // Russian roulette for continuation
                float luminance = Luminance(throughput);
                if (luminance < 1e-6f) break;

                float q = float.Min(luminance, RussianRouletteThreshold);
                if (sampler.Next1D() >= q) break;

                // Sample the BSDF to choose a bounce direction
                if (!mat.Sample(wo, hit, sampler, out Vec3 nextWi, out float pdf, out Vec3 f))
                    break;

                float absCos = float.Abs(Vec3.Dot(nextWi, hit.Normal));
                if (pdf <= 0f || absCos == 0f) break;

                throughput = Vec3.Hadamard(throughput, f * (absCos / (pdf * q)));
                mediumSA   = MediumAfterBounce(mat, nextWi, hit, mediumSA);
                ray        = new Ray(hit.Point, nextWi);
            }
            else
            {
                // Specular/glass: follow without storing
                if (!mat.Sample(wo, hit, sampler, out Vec3 nextWi, out float pdf, out Vec3 f))
                    break;

                float absCos = float.Abs(Vec3.Dot(nextWi, hit.Normal));
                if (pdf <= 0f || absCos == 0f) break;

                throughput = Vec3.Hadamard(throughput, f * (absCos / pdf));
                mediumSA   = MediumAfterBounce(mat, nextWi, hit, mediumSA);
                ray        = new Ray(hit.Point, nextWi);
            }
        }
    }

    // ── Phase 4: Gather ───────────────────────────────────────────────────────

    private static void GatherPhotons(
        SppmPixelState[] states, int count,
        PhotonHashGrid grid, Scene.Scene scene,
        ParallelOptions opts,
        ThreadLocal<List<int>> tlList)
    {
        Parallel.For(0, count, opts, i =>
        {
            var state = states[i];
            if (!state.IsValid) return;

            var nearby = tlList.Value!;
            grid.GatherIndices(state.Position, state.Radius, nearby);

            Vec3 newFlux   = Vec3.Zero;
            int  newPhotons = 0;

            foreach (int pIdx in nearby)
            {
                ref readonly var photon = ref grid.GetPhoton(pIdx);

                // Evaluate BSDF at the hit point: wi = -photon.Wi (pointing toward photon src)
                Vec3 wiLocal = (-photon.Wi).Normalized();
                Vec3 f       = state.Hit.Material.Evaluate(state.Wo, wiLocal, state.Hit);
                if (f.NearZero()) continue;

                // Accumulate: camera-throughput × BSDF × photon-power
                newFlux += Vec3.Hadamard(Vec3.Hadamard(state.CameraPathThroughput, f), photon.Power);
                newPhotons++;
            }

            state.NewPhotons = newPhotons;
            state.NewFlux    = newFlux;
        });
    }

    // ── Phase 5: Progressive update ───────────────────────────────────────────

    private static void UpdateStates(
        SppmPixelState[] states, int count,
        float alpha, ParallelOptions opts)
    {
        Parallel.For(0, count, opts, i =>
        {
            var state = states[i];

            // Accumulate direct light regardless of photon hits
            state.DirectLightSum        += state.DirectLight;
            state.DirectLightIterations += 1;

            if (!state.IsValid || state.NewPhotons == 0) return;

            // SPPM radius-reduction and τ-accumulation formula:
            //   N'   = N + α × ΔN
            //   scale = N' / (N + ΔN)       ∈ (α, 1)
            //   R'   = R × √scale
            //   τ'   = (τ + ΔΦ) × scale
            double N_old  = state.N;
            int    dN     = state.NewPhotons;
            double N_new  = N_old + alpha * dN;
            double scale  = (N_old + dN) > 0.0 ? N_new / (N_old + dN) : alpha;

            state.N      = N_new;
            state.Radius = state.Radius * (float)System.Math.Sqrt(scale);
            state.Tau    = (state.Tau + state.NewFlux) * (float)scale;
        });
    }

    // ── Phase 6: Reconstruct ──────────────────────────────────────────────────

    private static Vec3[] ReconstructFrame(
        SppmPixelState[] states, int width, int height,
        long totalPhotonsEmitted)
    {
        var frame = new Vec3[width * height];
        float invPhotons = totalPhotonsEmitted > 0 ? 1f / totalPhotonsEmitted : 0f;

        Parallel.For(0, width * height, i =>
        {
            var state = states[i];

            // Direct light: running average across iterations
            Vec3 direct = state.DirectLightIterations > 0
                ? state.DirectLightSum / state.DirectLightIterations
                : Vec3.Zero;

            if (!state.IsValid || state.N <= 0.0)
            {
                frame[i] = direct;
                return;
            }

            // Indirect photon-mapping estimate:
            //   L_indirect = τ / (π × R² × total_photons_emitted)
            float denom    = MathUtil.Pi * state.Radius * state.Radius * invPhotons;
            Vec3  indirect = denom > 0f ? state.Tau / denom : Vec3.Zero;

            frame[i] = direct + indirect;
        });

        return frame;
    }

    // ── Direct illumination (NEE) ─────────────────────────────────────────────

    /// <summary>
    /// Multiple-importance-sampled direct illumination at a diffuse hit point.
    /// Identical to the path tracer's EstimateDirect, so caustics are NOT double-counted:
    /// the photon map handles specular→diffuse (S·D) paths; NEE handles direct D paths.
    /// </summary>
    private static Vec3 EstimateDirect(
        in HitRecord hit, in Vec3 wo,
        Scene.Scene scene, Sampler sampler, float time)
    {
        if (scene.Lights.Count == 0) return Vec3.Zero;

        // Uniform light selection
        int   nLights  = scene.Lights.Count;
        int   li       = (int)(sampler.Next1D() * nLights);
        if (li >= nLights) li = nLights - 1;
        var   light    = scene.Lights[li];

        Vec3  L = Vec3.Zero;

        // ── Light-source sample ───────────────────────────────────────────────
        var ls = light.Sample(hit.Point, sampler);
        if (ls.Pdf > 0f && !ls.Radiance.NearZero())
        {
            if (!scene.World.Hit(new Ray(hit.Point, ls.Wi, time), 1e-4f, ls.Distance - 1e-4f, out _))
            {
                Vec3 f       = hit.Material.Evaluate(wo, ls.Wi, hit);
                float absCos = float.Abs(Vec3.Dot(ls.Wi, hit.Normal));
                if (!f.NearZero() && absCos > 0f)
                {
                    float bsdfPdf  = hit.Material.Pdf(wo, ls.Wi, hit);
                    float weight   = PowerHeuristic(ls.Pdf, bsdfPdf);
                    L += Vec3.Hadamard(f, ls.Radiance) * (absCos * weight / ls.Pdf);
                }
            }
        }

        // ── BSDF sample ───────────────────────────────────────────────────────
        if (hit.Material.Sample(wo, hit, sampler, out Vec3 wi, out float bPdf, out Vec3 bf))
        {
            float absCos = float.Abs(Vec3.Dot(wi, hit.Normal));
            if (!bf.NearZero() && absCos > 0f && bPdf > 0f)
            {
                float lPdf = light.Pdf(hit.Point, wi);
                if (lPdf > 0f)
                {
                    var shadowRay = new Ray(hit.Point, wi, time);
                    if (!scene.World.Hit(shadowRay, 1e-4f, float.PositiveInfinity, out var shadowHit))
                    {
                        // Ray escaped – no light hit
                    }
                    else
                    {
                        Vec3 Le = shadowHit.Material.Emitted(shadowRay, shadowHit);
                        if (!Le.NearZero())
                        {
                            float weight = PowerHeuristic(bPdf, lPdf);
                            L += Vec3.Hadamard(bf, Le) * (absCos * weight / bPdf);
                        }
                    }
                }
            }
        }

        return L * nLights; // compensate for light selection probability
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Vec3 MediumTransmittance(in Vec3 sigmaA, float t)
    {
        if (sigmaA.NearZero()) return Vec3.One;
        return new Vec3(
            float.Exp(-sigmaA.X * t),
            float.Exp(-sigmaA.Y * t),
            float.Exp(-sigmaA.Z * t));
    }

    private static Vec3 MediumAfterBounce(IMaterial mat, in Vec3 wi, in HitRecord hit, in Vec3 current)
    {
        if (mat is Dielectric d)
        {
            // Entering medium: frontFace + transmitted (wi into surface)
            bool transmitted = Vec3.Dot(wi, hit.Normal) < 0f;
            return transmitted ? (hit.FrontFace ? d.SigmaA : Vec3.Zero) : Vec3.Zero;
        }
        return current;
    }

    private static float Luminance(in Vec3 v) => 0.2126f * v.X + 0.7152f * v.Y + 0.0722f * v.Z;

    private static float PowerHeuristic(float pdf0, float pdf1)
    {
        float p0 = pdf0 * pdf0;
        float p1 = pdf1 * pdf1;
        return p0 / (p0 + p1 + 1e-10f);
    }

    private static float MaxCurrentRadius(SppmPixelState[] states, int count)
    {
        float max = 1e-6f;
        for (int i = 0; i < count; i++)
            if (states[i].Radius > max) max = states[i].Radius;
        return max;
    }

    private static float AverageRadius(SppmPixelState[] states, int count)
    {
        double sum = 0.0;
        for (int i = 0; i < count; i++) sum += states[i].Radius;
        return (float)(sum / count);
    }
}
