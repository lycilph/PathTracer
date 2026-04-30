using Core.Math;

namespace Core.Rendering;

/// <summary>
/// Progressive accumulation buffer storing linear RGB sums and per-pixel sample counts.
/// Partial image is always valid because each pixel tracks its own sample count.
/// </summary>
public sealed class AccumulationBuffer
{
    public int Width { get; }
    public int Height { get; }

    // Sum of samples in linear space
    private readonly float[] _sumR;
    private readonly float[] _sumG;
    private readonly float[] _sumB;

    // Per-pixel sample count (allows partial validity during a pass)
    private readonly int[] _spp;

    public AccumulationBuffer(int width, int height)
    {
        Width = width;
        Height = height;

        int n = checked(width * height);
        _sumR = new float[n];
        _sumG = new float[n];
        _sumB = new float[n];
        _spp = new int[n];
    }

    public void Clear()
    {
        Array.Clear(_sumR);
        Array.Clear(_sumG);
        Array.Clear(_sumB);
        Array.Clear(_spp);
    }

    public int GetSppMinMax(out int max)
    {
        int min = int.MaxValue;
        int localMax = 0;

        for (int i = 0; i < _spp.Length; i++)
        {
            int s = _spp[i];
            if (s < min) min = s;
            if (s > localMax) localMax = s;
        }

        if (min == int.MaxValue) min = 0;
        max = localMax;
        return min;
    }

    public void AddSample(int x, int y, in Vec3 linearRgb)
    {
        int idx = y * Width + x;

        _sumR[idx] += linearRgb.X;
        _sumG[idx] += linearRgb.Y;
        _sumB[idx] += linearRgb.Z;

        _spp[idx] += 1;
    }

    public Vec3 GetAverage(int x, int y)
    {
        int idx = y * Width + x;
        int s = _spp[idx];
        if (s <= 0) return Vec3.Zero;

        float inv = 1f / s;
        return new Vec3(_sumR[idx] * inv, _sumG[idx] * inv, _sumB[idx] * inv);
    }

    public int GetSpp(int x, int y)
    {
        int idx = y * Width + x;
        return _spp[idx];
    }

    public float ComputeAverageLuminance()
    {
        double sum = 0.0;
        long count = 0;

        for (int i = 0; i < _spp.Length; i++)
        {
            int s = _spp[i];
            if (s <= 0) continue;

            float inv = 1f / s;
            float r = _sumR[i] * inv;
            float g = _sumG[i] * inv;
            float b = _sumB[i] * inv;

            // Simple luminance estimate (linear Rec.709)
            float y = 0.2126f * r + 0.7152f * g + 0.0722f * b;
            sum += y;
            count++;
        }

        return count > 0 ? (float)(sum / count) : 0f;
    }
}