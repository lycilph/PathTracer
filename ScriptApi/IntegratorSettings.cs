namespace ScriptApi;

/// <summary>
/// Configures which integrator to use for rendering and its parameters.
/// Use the static factory methods to create settings.
/// </summary>
public sealed class IntegratorSettings
{
    /// <summary>The type of integrator to use.</summary>
    public IntegratorType Type { get; }

    // ── Path tracing settings ─────────────────────────────────────────────────

    /// <summary>
    /// Minimum path depth before Russian Roulette termination begins.
    /// Only used when <see cref="Type"/> is
    /// <see cref="IntegratorType.PathTracing"/>.
    /// </summary>
    public int MinDepth { get; }

    /// <summary>
    /// Hard maximum path depth.
    /// Only used when <see cref="Type"/> is
    /// <see cref="IntegratorType.PathTracing"/>.
    /// </summary>
    public int MaxDepth { get; }

    // ── Photon mapping settings ───────────────────────────────────────────────

    /// <summary>
    /// Number of photons to emit per PPM pass.
    /// Only used when <see cref="Type"/> is
    /// <see cref="IntegratorType.PhotonMapping"/>.
    /// </summary>
    public int PhotonsPerPass { get; }

    /// <summary>
    /// Initial search radius for radiance estimation.
    /// Shrinks each pass according to the PPM update rule.
    /// Only used when <see cref="Type"/> is
    /// <see cref="IntegratorType.PhotonMapping"/>.
    /// </summary>
    public double InitialRadius { get; }

    /// <summary>
    /// Number of nearest photons to use in the radiance estimate.
    /// Higher values reduce noise but increase bias.
    /// Only used when <see cref="Type"/> is
    /// <see cref="IntegratorType.PhotonMapping"/>.
    /// </summary>
    public int KNearest { get; }

    /// <summary>
    /// PPM radius contraction factor α in (0, 1].
    /// Controls how quickly the radius shrinks each pass.
    /// Spec recommends ≈ 0.7 (§3.11.4).
    /// Only used when <see cref="Type"/> is
    /// <see cref="IntegratorType.PhotonMapping"/>.
    /// </summary>
    public double Alpha { get; }

    /// <summary>
    /// Maximum number of PPM passes to run.
    /// 0 means run indefinitely until cancelled.
    /// Only used when <see cref="Type"/> is
    /// <see cref="IntegratorType.PhotonMapping"/>.
    /// </summary>
    public int MaxPasses { get; }

    private IntegratorSettings(
        IntegratorType type,
        int minDepth,
        int maxDepth,
        int photonsPerPass,
        double initialRadius,
        int kNearest,
        double alpha,
        int maxPasses)
    {
        Type = type;
        MinDepth = minDepth;
        MaxDepth = maxDepth;
        PhotonsPerPass = photonsPerPass;
        InitialRadius = initialRadius;
        KNearest = kNearest;
        Alpha = alpha;
        MaxPasses = maxPasses;
    }

    // ── Factory methods ───────────────────────────────────────────────────────

    /// <summary>
    /// Creates path tracing settings with the given parameters.
    /// This is the default integrator — suitable for most scenes.
    /// </summary>
    /// <param name="minDepth">
    /// Minimum bounces before Russian Roulette. Default 3.
    /// </param>
    /// <param name="maxDepth">
    /// Maximum path depth. Default 50.
    /// </param>
    public static IntegratorSettings PathTracing(
        int minDepth = 3,
        int maxDepth = 50)
        => new(
            IntegratorType.PathTracing,
            minDepth, maxDepth,
            photonsPerPass: 0,
            initialRadius: 0,
            kNearest: 0,
            alpha: 0,
            maxPasses: 0);

    /// <summary>
    /// Creates photon mapping settings with the given parameters.
    /// Use for scenes with caustics or complex indirect illumination.
    /// </summary>
    /// <param name="photonsPerPass">
    /// Number of photons to emit per PPM pass. Default 100,000.
    /// </param>
    /// <param name="initialRadius">
    /// Starting search radius for radiance estimation. Default 0.1.
    /// Should be set relative to scene scale.
    /// </param>
    /// <param name="kNearest">
    /// Photons to use in the radiance estimate. Default 50.
    /// </param>
    /// <param name="alpha">
    /// Radius contraction factor. Default 0.7 per spec (§3.11.4).
    /// </param>
    /// <param name="maxPasses">
    /// Maximum PPM passes. 0 = run until cancelled. Default 0.
    /// </param>
    public static IntegratorSettings PhotonMapping(
        int photonsPerPass = 100_000,
        double initialRadius = 0.1,
        int kNearest = 50,
        double alpha = 0.7,
        int maxPasses = 0)
        => new(
            IntegratorType.PhotonMapping,
            minDepth: 0,
            maxDepth: 0,
            photonsPerPass,
            initialRadius,
            kNearest,
            alpha,
            maxPasses);
}

/// <summary>The type of integrator to use for rendering.</summary>
public enum IntegratorType
{
    /// <summary>
    /// Unidirectional path tracing with MIS direct lighting.
    /// Good general-purpose integrator.
    /// </summary>
    PathTracing,

    /// <summary>
    /// Progressive Photon Mapping — combines MIS direct lighting
    /// with photon map density estimation for indirect and caustic
    /// illumination. Better for caustics and complex indirect light.
    /// </summary>
    PhotonMapping
}