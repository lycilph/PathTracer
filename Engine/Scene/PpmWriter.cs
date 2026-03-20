using Engine.Rendering;

namespace Engine.Scene;

/// <summary>
/// Writes a FrameBuffer to a PPM image file (Portable Pixmap, plain text).
/// PPM requires no external dependencies and can be opened by most image viewers.
/// </summary>
public static class PpmWriter
{
    /// <summary>
    /// Writes the current frame buffer contents to a PPM file.
    /// </summary>
    /// <param name="frameBuffer">The buffer to write.</param>
    /// <param name="path">Output file path. Should end in .ppm</param>
    public static void Write(FrameBuffer frameBuffer, string path)
    {
        using var writer = new StreamWriter(path);

        // PPM header: magic number, dimensions, max value
        writer.WriteLine("P3");
        writer.WriteLine($"{frameBuffer.Width} {frameBuffer.Height}");
        writer.WriteLine("255");

        for (var y = 0; y < frameBuffer.Height; y++)
        {
            for (var x = 0; x < frameBuffer.Width; x++)
            {
                var (r, g, b) = frameBuffer.GetDisplayPixel(x, y);
                var ri = (int)Math.Clamp(r * 255.999, 0, 255);
                var gi = (int)Math.Clamp(g * 255.999, 0, 255);
                var bi = (int)Math.Clamp(b * 255.999, 0, 255);
                writer.WriteLine($"{ri} {gi} {bi}");
            }
        }
    }
}