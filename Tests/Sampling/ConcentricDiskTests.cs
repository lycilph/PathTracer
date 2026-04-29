using Core.Sampling;

namespace Tests.Sampling;

public class ConcentricDiskTests
{
    [Fact]
    public void ConcentricSampleDisk_StaysInsideUnitDisk()
    {
        // A small deterministic sweep
        for (int i = 0; i <= 50; i++)
            for (int j = 0; j <= 50; j++)
            {
                float u1 = i / 50f;
                float u2 = j / 50f;

                var p = SamplingUtil.ConcentricSampleDisk(u1, u2);
                float r2 = p.X * p.X + p.Y * p.Y;
                Assert.True(r2 <= 1.0001f);
            }
    }
}