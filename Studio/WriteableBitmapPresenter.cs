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

    public void UpdateTileFromRawRgb(
        Vec3[] linearRgb,
        int width,
        int height,
        int x0,
        int y0,
        int w,
        int h,
        Func<Vec3, Vec3>? transform = null)
    {
        // Ensure caller dimensions match presenter
        if (width != _width || height != _height)
            throw new InvalidOperationException($"Presenter size {_width}x{_height} != buffer size {width}x{height}");

        // Clamp rect to bounds (defensive)
        if (w <= 0 || h <= 0) return;
        if (x0 < 0) { w += x0; x0 = 0; }
        if (y0 < 0) { h += y0; y0 = 0; }
        if (x0 + w > _width) w = _width - x0;
        if (y0 + h > _height) h = _height - y0;
        if (w <= 0 || h <= 0) return;

        for (int y = y0; y < y0 + h; y++)
        {
            int rowOffset = y * _width;
            for (int x = x0; x < x0 + w; x++)
            {
                Vec3 c = linearRgb[rowOffset + x];
                if (transform != null) c = transform(c);

                // Gamma preview
                c = new Vec3(
                    float.Sqrt(MathUtil.Clamp(c.X, 0f, 1f)),
                    float.Sqrt(MathUtil.Clamp(c.Y, 0f, 1f)),
                    float.Sqrt(MathUtil.Clamp(c.Z, 0f, 1f)));

                int idx = (rowOffset + x) * 4;
                _pixels[idx + 0] = (byte)(255.999f * MathUtil.Clamp(c.Z, 0f, 1f)); // B
                _pixels[idx + 1] = (byte)(255.999f * MathUtil.Clamp(c.Y, 0f, 1f)); // G
                _pixels[idx + 2] = (byte)(255.999f * MathUtil.Clamp(c.X, 0f, 1f)); // R
                _pixels[idx + 3] = 255;
            }
        }

        Bitmap.Lock();
        try
        {
            var rect = new Int32Rect(x0, y0, w, h);
            int stride = _width * 4;
            int bufferOffset = (y0 * _width + x0) * 4;

            Bitmap.WritePixels(rect, _pixels, stride, bufferOffset);
        }
        finally
        {
            Bitmap.Unlock();
        }
    }
}