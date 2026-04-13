using Core.Math;

namespace Core.Rendering;

/// <summary>
/// Color utilities. In early milestones we represent RGB colors using Vec3 (linear RGB).
/// Output conversion to display space is handled here.
/// </summary>
public static class ColorUtil
{
    /// <summary>
    /// Applies a simple gamma correction for preview (gamma=2.0). Later milestones will add HDR + tone mapping.
    /// Input and output are both assumed to be in [0, +inf) and the result is clamped to [0,1].
    /// </summary>
    public static Vec3 Gamma2(in Vec3 linear)
    {
        static float G(float x)
        {
            if (x <= 0f) return 0f;
            return float.Sqrt(x);
        }

        return new Vec3(G(linear.X), G(linear.Y), G(linear.Z));
    }

    public static Vec3 Clamp01(in Vec3 c)
        => new Vec3(MathUtil.Saturate(c.X), MathUtil.Saturate(c.Y), MathUtil.Saturate(c.Z));

    /// <summary>
    /// Converts a linear RGB color in [0,1] to 8-bit per channel.
    /// </summary>
    public static (byte r, byte g, byte b) ToRgb8(in Vec3 linear01, bool applyGamma2 = true)
    {
        var c = applyGamma2 ? Gamma2(linear01) : linear01;
        c = Clamp01(c);

        static byte ToByte(float x)
        {
            // Map [0,1] -> [0,255]
            int v = (int)(255.999f * x);
            if (v < 0) v = 0;
            if (v > 255) v = 255;
            return (byte)v;
        }

        return (ToByte(c.X), ToByte(c.Y), ToByte(c.Z));
    }
}
