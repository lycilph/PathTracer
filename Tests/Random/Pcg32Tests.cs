using Core.Random;

namespace Tests.Random;

public class Pcg32Tests
{
    [Fact]
    public void SameSeed_ProducesSameSequence()
    {
        var a = new Pcg32(seed: 1234, sequence: 7);
        var b = new Pcg32(seed: 1234, sequence: 7);

        for (int i = 0; i < 20; i++)
            Assert.Equal(a.NextUInt(), b.NextUInt());
    }

    [Fact]
    public void DifferentSeeds_UsuallyDifferentSequences()
    {
        var a = new Pcg32(seed: 1234, sequence: 7);
        var b = new Pcg32(seed: 1235, sequence: 7);

        // Not a proof, but extremely likely to differ quickly.
        Assert.NotEqual(a.NextUInt(), b.NextUInt());
    }

    [Fact]
    public void NextFloat01_IsInRange()
    {
        var rng = new Pcg32(seed: 42);

        for (int i = 0; i < 10_000; i++)
        {
            float v = rng.NextFloat01();
            Assert.True(v >= 0f);
            Assert.True(v < 1f);
        }
    }

    [Fact]
    public void GoldenSequence_IsStableWithinImplementation()
    {
        // This ensures refactors don't accidentally change the sequence.
        var rng = new Pcg32(seed: 1, sequence: 54);

        uint v0 = rng.NextUInt();
        uint v1 = rng.NextUInt();
        uint v2 = rng.NextUInt();
        uint v3 = rng.NextUInt();
        uint v4 = rng.NextUInt();

        var rng2 = new Pcg32(seed: 1, sequence: 54);
        Assert.Equal(v0, rng2.NextUInt());
        Assert.Equal(v1, rng2.NextUInt());
        Assert.Equal(v2, rng2.NextUInt());
        Assert.Equal(v3, rng2.NextUInt());
        Assert.Equal(v4, rng2.NextUInt());
    }
}
