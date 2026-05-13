using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Core.Math;
using Core.Rendering;

namespace Studio;

public sealed class WriteableBitmapPresenter
{
    public WriteableBitmap Bitmap { get; private set; }

    private byte[] _pixels;
    private int _width;
    private int _height;

    private byte[]? _frameBytes;

    public WriteableBitmapPresenter(int width, int height)
    {
        _width = width;
        _height = height;
        Bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
        _pixels = new byte[width * height * 4];
    }

    public void Resize(int width, int height)
    {
        _width = width;
        _height = height;
        Bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
        _pixels = new byte[width * height * 4];
    }

    public void UpdateTile(AccumulationBuffer accum, int x0, int y0, int w, int h)
    {
        for (int y = y0; y < y0 + h; y++)
        {
            int rowOffset = y * _width;
            for (int x = x0; x < x0 + w; x++)
            {
                Vec3 c = accum.GetAverage(x, y);

                // Gamma 2.0 preview (same as your ColorUtil behavior)
                c = new Vec3(float.Sqrt(MathUtil.Clamp(c.X, 0f, 1f)),
                             float.Sqrt(MathUtil.Clamp(c.Y, 0f, 1f)),
                             float.Sqrt(MathUtil.Clamp(c.Z, 0f, 1f)));

                int idx = (rowOffset + x) * 4;

                // BGRA
                _pixels[idx + 0] = (byte)(255.999f * MathUtil.Clamp(c.Z, 0f, 1f));
                _pixels[idx + 1] = (byte)(255.999f * MathUtil.Clamp(c.Y, 0f, 1f));
                _pixels[idx + 2] = (byte)(255.999f * MathUtil.Clamp(c.X, 0f, 1f));
                _pixels[idx + 3] = 255;
            }
        }

        Bitmap.Lock();
        try
        {
            var rect = new Int32Rect(x0, y0, w, h);
            Bitmap.WritePixels(rect, _pixels, _width * 4, y0 * _width * 4 + x0 * 4);
        }
        finally
        {
            Bitmap.Unlock();
        }
    }

    /// <summary>
    /// Replaces the entire display bitmap with the linear-light <paramref name="frame"/>
    /// produced by <see cref="Core.Rendering.Sppm.SppmRenderer"/> after each iteration.
    /// Applies √ gamma encoding and clamps to [0, 1] before writing.
    ///
    /// Must be called on the UI thread (or dispatched there).
    /// </summary>
    public void UpdateFullFrame(Vec3[] frame)
    {
        if (frame.Length != _width * _height)
            throw new ArgumentException(
                $"Frame length {frame.Length} does not match {_width}×{_height} = {_width * _height}.",
                nameof(frame));

        // Reuse or allocate byte backing buffer (BGRA32, 4 bytes per pixel)
        int byteCount = _width * _height * 4;
        if (_frameBytes is null || _frameBytes.Length != byteCount)
            _frameBytes = new byte[byteCount];

        // Encode linear → gamma (√ approximation) and convert to BGRA bytes
        for (int i = 0; i < frame.Length; i++)
        {
            Vec3 c = frame[i];
            float r = float.Sqrt(float.Clamp(c.X, 0f, 1f));
            float g = float.Sqrt(float.Clamp(c.Y, 0f, 1f));
            float b = float.Sqrt(float.Clamp(c.Z, 0f, 1f));

            int o = i * 4;
            _frameBytes[o + 0] = (byte)(b * 255.999f); // B
            _frameBytes[o + 1] = (byte)(g * 255.999f); // G
            _frameBytes[o + 2] = (byte)(r * 255.999f); // R
            _frameBytes[o + 3] = 255;                  // A
        }

        Bitmap.Lock();
        try
        {
            Bitmap.WritePixels(
                new Int32Rect(0, 0, _width, _height),
                _frameBytes,
                _width * 4,
                0);
            Bitmap.AddDirtyRect(new Int32Rect(0, 0, _width, _height));
        }
        finally
        {
            Bitmap.Unlock();
        }
    }
}