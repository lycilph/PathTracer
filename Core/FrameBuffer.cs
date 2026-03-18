namespace Core;

/// <summary>
/// Thread-safe HDR accumulation buffer (§3.10).
/// Each pixel accumulates radiance samples; the display value is the running mean.
/// </summary>
public sealed class FrameBuffer
{
    private readonly double[] _sumR;
    private readonly double[] _sumG;
    private readonly double[] _sumB;
    private readonly int[] _counts;
    private readonly object[] _locks;

    public int Width { get; }
    public int Height { get; }

    public FrameBuffer(int width, int height)
    {
        Width = width;
        Height = height;
        var n = width * height;
        _sumR = new double[n];
        _sumG = new double[n];
        _sumB = new double[n];
        _counts = new int[n];
        _locks = new object[n];
        for (var i = 0; i < n; i++)
            _locks[i] = new object();
    }

    /// <summary>
    /// Adds one radiance sample to pixel (x, y).
    /// Thread-safe — may be called concurrently from multiple render threads.
    /// </summary>
    /// <param name="x">Pixel column, zero-based.</param>
    /// <param name="y">Pixel row, zero-based.</param>
    /// <param name="radiance">HDR radiance sample to accumulate.</param>
    public void AddSample(int x, int y, Vector3 radiance)
    {
        var idx = y * Width + x;
        lock (_locks[idx])
        {
            _sumR[idx] += radiance.X;
            _sumG[idx] += radiance.Y;
            _sumB[idx] += radiance.Z;
            _counts[idx] += 1;
        }
    }

    /// <summary>
    /// Returns the current mean radiance at pixel (x, y).
    /// Returns zero for pixels with no samples yet.
    /// </summary>
    public Vector3 GetPixelRadiance(int x, int y)
    {
        var idx = y * Width + x;
        lock (_locks[idx])
        {
            var count = _counts[idx];
            if (count == 0) return Vector3.Zero;
            return new Vector3(
                _sumR[idx] / count,
                _sumG[idx] / count,
                _sumB[idx] / count);
        }
    }

    /// <summary>
    /// Returns the number of samples accumulated at pixel (x, y).
    /// </summary>
    public int GetSampleCount(int x, int y)
    {
        var idx = y * Width + x;
        lock (_locks[idx])
            return _counts[idx];
    }

    /// <summary>Resets all pixels to zero samples.</summary>
    public void Clear()
    {
        var n = Width * Height;
        for (var i = 0; i < n; i++)
        {
            lock (_locks[i])
            {
                _sumR[i] = 0;
                _sumG[i] = 0;
                _sumB[i] = 0;
                _counts[i] = 0;
            }
        }
    }

    /// <summary>
    /// Converts the mean radiance at pixel (x, y) to a display-ready sRGB
    /// value using ACES filmic tone mapping and gamma correction (§3.10).
    /// </summary>
    /// <returns>RGB components each in [0, 1].</returns>
    public (double R, double G, double B) GetDisplayPixel(int x, int y)
    {
        var hdr = GetPixelRadiance(x, y);
        var r = GammaCorrect(AcesFilmic(hdr.X));
        var g = GammaCorrect(AcesFilmic(hdr.Y));
        var b = GammaCorrect(AcesFilmic(hdr.Z));
        return (r, g, b);
    }

    /// <summary>
    /// ACES filmic tone mapping operator (§3.10.2).
    /// Maps HDR luminance to [0, 1] with a filmic S-curve.
    /// L_display = (L·(2.51L + 0.03)) / (L·(2.43L + 0.59) + 0.14)
    /// </summary>
    private static double AcesFilmic(double x)
    {
        x = Math.Max(0, x);
        return Math.Clamp(
            (x * (2.51 * x + 0.03)) / (x * (2.43 * x + 0.59) + 0.14),
            0.0, 1.0);
    }

    /// <summary>
    /// Gamma correction: linear → sRGB (§3.10.3).
    /// L_sRGB = pow(L_linear, 1/2.2)
    /// </summary>
    private static double GammaCorrect(double linear)
        => Math.Pow(Math.Max(0, linear), 1.0 / 2.2);
}