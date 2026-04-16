using Core.Rendering;

namespace Tests.Rendering;

public class MisTests
{
    [Fact]
    public void PowerHeuristic_IsSymmetric()
    {
        float a = 0.2f;
        float b = 0.8f;
        float wa = Mis.PowerHeuristic(a, b);
        float wb = Mis.PowerHeuristic(b, a);
        Assert.InRange(wa + wb, 0.9999f, 1.0001f);
    }

    [Fact]
    public void PowerHeuristic_HandlesZeros()
    {
        Assert.Equal(0f, Mis.PowerHeuristic(0f, 0f));
        Assert.Equal(1f, Mis.PowerHeuristic(1f, 0f));
        Assert.Equal(0f, Mis.PowerHeuristic(0f, 1f));
    }
}