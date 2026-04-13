using Core.Random;

namespace Tests.Random;

public class SeedHashTests
{
    [Fact]
    public void PixelSampleSeed_IsDeterministic()
    {
        ulong s1 = SeedHash.PixelSampleSeed(10, 20, 3, 999);
        ulong s2 = SeedHash.PixelSampleSeed(10, 20, 3, 999);
        Assert.Equal(s1, s2);
    }

    [Fact]
    public void PixelSampleSeed_ChangesWithInputs()
    {
        ulong a = SeedHash.PixelSampleSeed(10, 20, 3, 999);
        ulong b = SeedHash.PixelSampleSeed(11, 20, 3, 999);
        ulong c = SeedHash.PixelSampleSeed(10, 21, 3, 999);
        ulong d = SeedHash.PixelSampleSeed(10, 20, 4, 999);

        Assert.NotEqual(a, b);
        Assert.NotEqual(a, c);
        Assert.NotEqual(a, d);
    }
}
