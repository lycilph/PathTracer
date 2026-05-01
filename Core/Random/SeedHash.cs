namespace Core.Random;

/// <summary>
/// Helpers for deriving well-distributed deterministic seeds.
/// </summary>
public static class SeedHash
{
    /// <summary>
    /// SplitMix64: commonly used to scramble seeds into well-distributed 64-bit values.
    /// </summary>
    public static ulong SplitMix64(ulong x)
    {
        x += 0x9E3779B97F4A7C15ul;
        x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9ul;
        x = (x ^ (x >> 27)) * 0x94D049BB133111EBul;
        return x ^ (x >> 31);
    }

    /// <summary>
    /// Deterministic seed for a given pixel coordinate and sample index.
    /// Enables reproducible results independent of thread scheduling.
    /// </summary>
    public static ulong PixelSampleSeed(int x, int y, int sampleIndex, ulong baseSeed)
    {
        unchecked
        {
            ulong h = baseSeed;
            h ^= (ulong)(uint)x * 0xA511E9B3u;
            h ^= (ulong)(uint)y * 0x63D83595u;
            h ^= (ulong)(uint)sampleIndex * 0x9E3779B9u;
            return SplitMix64(h);
        }
    }

    /// <summary>
    /// Mixes a 64-bit value into a well-distributed 64-bit hash.
    /// Useful for turning structured seeds into RNG seeds.
    /// </summary>
    public static ulong Mix64(ulong x)
    {
        x += 0x9E3779B97F4A7C15UL;
        x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
        x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
        return x ^ (x >> 31);
    }

    /// <summary>
    /// Hash two 64-bit values into one 64-bit seed.
    /// </summary>
    public static ulong Hash64(ulong a, ulong b)
    {
        // Combine + mix; stable and deterministic
        return Mix64(a ^ Mix64(b));
    }

    /// <summary>
    /// Hash three 64-bit values into one 64-bit seed.
    /// </summary>
    public static ulong Hash64(ulong a, ulong b, ulong c)
    {
        return Mix64(a ^ Mix64(b) ^ Mix64(c));
    }
}
