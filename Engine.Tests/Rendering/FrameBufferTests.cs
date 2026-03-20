using Core.Algebra;
using Engine.Rendering;
using FluentAssertions;

namespace Engine.Tests.Rendering;

public class FrameBufferTests
{
    [Fact]
    public void GetPixelRadiance_NoSamples_ReturnsZero()
    {
        var fb = new FrameBuffer(10, 10);
        fb.GetPixelRadiance(5, 5).Should().Be(Vector3.Zero);
    }

    [Fact]
    public void AddSample_SingleSample_ReturnsExactValue()
    {
        var fb = new FrameBuffer(10, 10);
        var sample = new Vector3(0.5, 0.3, 0.8);
        fb.AddSample(3, 4, sample);

        var result = fb.GetPixelRadiance(3, 4);
        result.X.Should().BeApproximately(0.5, 1e-10);
        result.Y.Should().BeApproximately(0.3, 1e-10);
        result.Z.Should().BeApproximately(0.8, 1e-10);
    }

    [Fact]
    public void AddSample_MultipleSamples_ReturnsRunningMean()
    {
        var fb = new FrameBuffer(10, 10);
        fb.AddSample(0, 0, new Vector3(1.0, 0.0, 0.0));
        fb.AddSample(0, 0, new Vector3(0.0, 1.0, 0.0));
        fb.AddSample(0, 0, new Vector3(0.0, 0.0, 1.0));

        var result = fb.GetPixelRadiance(0, 0);
        result.X.Should().BeApproximately(1.0 / 3.0, 1e-10);
        result.Y.Should().BeApproximately(1.0 / 3.0, 1e-10);
        result.Z.Should().BeApproximately(1.0 / 3.0, 1e-10);
    }

    [Fact]
    public void GetSampleCount_TracksCorrectly()
    {
        var fb = new FrameBuffer(10, 10);
        fb.GetSampleCount(2, 2).Should().Be(0);
        fb.AddSample(2, 2, Vector3.One);
        fb.GetSampleCount(2, 2).Should().Be(1);
        fb.AddSample(2, 2, Vector3.One);
        fb.GetSampleCount(2, 2).Should().Be(2);
    }

    [Fact]
    public void Clear_ResetsAllPixels()
    {
        var fb = new FrameBuffer(10, 10);
        fb.AddSample(0, 0, Vector3.One);
        fb.AddSample(5, 5, Vector3.One);
        fb.Clear();

        fb.GetPixelRadiance(0, 0).Should().Be(Vector3.Zero);
        fb.GetPixelRadiance(5, 5).Should().Be(Vector3.Zero);
        fb.GetSampleCount(0, 0).Should().Be(0);
    }

    [Fact]
    public void GetDisplayPixel_BlackInput_ReturnsBlack()
    {
        var fb = new FrameBuffer(10, 10);
        fb.AddSample(0, 0, Vector3.Zero);

        var (r, g, b) = fb.GetDisplayPixel(0, 0);
        r.Should().BeApproximately(0.0, 1e-10);
        g.Should().BeApproximately(0.0, 1e-10);
        b.Should().BeApproximately(0.0, 1e-10);
    }

    [Fact]
    public void GetDisplayPixel_OutputAlwaysInZeroToOne()
    {
        var fb = new FrameBuffer(10, 10);

        // HDR values well above 1 should be tone-mapped into [0,1]
        fb.AddSample(0, 0, new Vector3(100, 50, 200));
        var (r, g, b) = fb.GetDisplayPixel(0, 0);

        r.Should().BeInRange(0.0, 1.0);
        g.Should().BeInRange(0.0, 1.0);
        b.Should().BeInRange(0.0, 1.0);
    }

    [Fact]
    public void AddSample_ConcurrentWrites_DoesNotCorruptData()
    {
        // Hammer the same pixel from many threads — total must be consistent
        var fb = new FrameBuffer(10, 10);
        var threads = Enumerable.Range(0, 16)
            .Select(_ => new Thread(() =>
            {
                for (var i = 0; i < 1000; i++)
                    fb.AddSample(5, 5, Vector3.One);
            }))
            .ToList();

        threads.ForEach(t => t.Start());
        threads.ForEach(t => t.Join());

        fb.GetSampleCount(5, 5).Should().Be(16 * 1000);
        fb.GetPixelRadiance(5, 5).X.Should().BeApproximately(1.0, 1e-10);
    }
}