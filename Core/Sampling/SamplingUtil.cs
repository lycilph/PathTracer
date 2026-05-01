using Core.Math;

namespace Core.Sampling;

/// <summary>
/// Sampling utilities.
/// </summary>
public static class SamplingUtil
{
    /// <summary>
    /// Shirley–Chiu concentric mapping from [0,1)^2 to a uniform unit disk.
    /// Returns (x,y) on disk with z = 0.
    /// </summary>
    public static Vec3 ConcentricSampleDisk(float u1, float u2)
    {
        // Map uniform random numbers to [-1,1]^2
        float sx = 2f * u1 - 1f;
        float sy = 2f * u2 - 1f;

        if (sx == 0f && sy == 0f)
            return new Vec3(0f, 0f, 0f);

        float r, theta;

        if (float.Abs(sx) > float.Abs(sy))
        {
            r = sx;
            theta = (MathUtil.Pi / 4f) * (sy / sx);
        }
        else
        {
            r = sy;
            theta = (MathUtil.Pi / 2f) - (MathUtil.Pi / 4f) * (sx / sy);
        }

        float dx = r * float.Cos(theta);
        float dy = r * float.Sin(theta);
        return new Vec3(dx, dy, 0f);
    }
    
    // Cosine-weighted hemisphere around +Z(local space)
    public static Vec3 CosineSampleHemisphere(float u1, float u2)
    {
        // concentric disk then lift to hemisphere
        var d = ConcentricSampleDisk(u1, u2);
        float z = float.Sqrt(float.Max(0f, 1f - d.X * d.X - d.Y * d.Y));
        return new Vec3(d.X, d.Y, z);
    }

    // Build orthonormal basis from normal
    public static (Vec3 t, Vec3 b, Vec3 n) MakeBasis(in Vec3 n)
    {
        var nn = n.Normalized();
        Vec3 a = float.Abs(nn.X) > 0.9f ? Vec3.UnitY : Vec3.UnitX;
        Vec3 b = Vec3.Cross(nn, a).Normalized();
        Vec3 t = Vec3.Cross(b, nn);
        return (t, b, nn);
    }

    public static Vec3 ToWorld(in Vec3 local, in Vec3 n)
    {
        var (t, b, nn) = MakeBasis(n);
        return (t * local.X + b * local.Y + nn * local.Z).Normalized();
    }

}