using Core.Math;
using Core.Rendering;

namespace Tests.Rendering;

public class RussianRouletteTests
{
    [Fact]
    public void ContinuationProbability_UsesMaxComponentAndClamps()
    {
        // Below min
        float p1 = RussianRoulette.ContinuationProbability(new Vec3(0.001f, 0.002f, 0.003f));
        Assert.InRange(p1, 0.0499f, 0.0501f);

        // Normal in range
        float p2 = RussianRoulette.ContinuationProbability(new Vec3(0.2f, 0.6f, 0.4f));
        Assert.InRange(p2, 0.5999f, 0.6001f);

        // Above max
        float p3 = RussianRoulette.ContinuationProbability(new Vec3(2f, 1f, 3f));
        Assert.InRange(p3, 0.9499f, 0.9501f);
    }
}