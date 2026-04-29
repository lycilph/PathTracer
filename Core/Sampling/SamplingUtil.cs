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
}