using Core.Math;

namespace Core.Rendering;

/// <summary>
/// Minimal PPM (P6) writer. Chosen for Milestone 1 to avoid extra dependencies.
/// Produces binary PPM with 8-bit RGB.
/// </summary>
public static class PpmWriter
{
    /// <summary>
    /// Writes a P6 PPM file.
    /// pixels must be width*height linear RGB colors.
    /// </summary>
    public static void WriteP6(string path, int width, int height, ReadOnlySpan<Vec3> pixels, bool applyGamma2 = true)
    {
        if (pixels.Length != width * height)
            throw new ArgumentException("Pixel buffer size mismatch", nameof(pixels));

        using var fs = File.Create(path);
        using var bw = new BinaryWriter(fs);

        // Header
        var header = $"P6\n{width} {height}\n255\n";
        bw.Write(System.Text.Encoding.ASCII.GetBytes(header));

        // Pixels: row-major, top-to-bottom
        for (int i = 0; i < pixels.Length; i++)
        {
            var (r, g, b) = ColorUtil.ToRgb8(pixels[i], applyGamma2);
            bw.Write(r);
            bw.Write(g);
            bw.Write(b);
        }
    }
}
