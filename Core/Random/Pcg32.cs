namespace Core.Random;

/// <summary>
/// PCG32 (XSH RR) random generator.
/// Deterministic and suitable for Monte Carlo sampling.
/// </summary>
public sealed class Pcg32 : IRng
{
    private ulong _state;
    private readonly ulong _inc;

    /// <param name="seed">Initial state seed.</param>
    /// <param name="sequence">Stream selector (must be distinct for independent streams).</param>
    public Pcg32(ulong seed, ulong sequence = 54ul)
    {
        _state = 0ul;
        _inc = (sequence << 1) | 1ul;
        NextUInt();
        _state += seed;
        NextUInt();
    }

    public uint NextUInt()
    {
        ulong oldState = _state;
        _state = unchecked(oldState * 6364136223846793005ul + _inc);

        uint xorshifted = (uint)(((oldState >> 18) ^ oldState) >> 27);
        int rot = (int)(oldState >> 59);

        // Rotate right
        return (xorshifted >> rot) | (xorshifted << ((-rot) & 31));
    }

    public float NextFloat01()
    {
        // Use the top 24 bits to form a float in [0,1)
        return (NextUInt() >> 8) * (1.0f / 16777216.0f);
    }
}
