using Core.Random;

namespace Core.Sampling;

/// <summary>
/// Simple sampler that provides a deterministic RNG per pixel/sample.
/// </summary>
public sealed class Sampler
{
    private readonly IRng _rng;

    public Sampler(IRng rng) => _rng = rng;

    public float Next1D() => _rng.NextFloat01();
}