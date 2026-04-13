using Core.Math;

namespace Core.Testing;

public static class ImageMetrics
{
    public static float Rmse(ReadOnlySpan<Vec3> a, ReadOnlySpan<Vec3> b)
    {
        double sum = 0.0;
        long n = (long)a.Length * 3;

        for (int i = 0; i < a.Length; i++)
        {
            var d = a[i] - b[i];
            sum += d.X * d.X + d.Y * d.Y + d.Z * d.Z;
        }

        return (float)System.Math.Sqrt(sum / n);
    }

    public static float PsnrFromRmse(float rmse, float peak = 1.0f)
    {
        if (rmse <= 0f) return float.PositiveInfinity;
        return 20f * (float)System.Math.Log10(peak / rmse);
    }

    public static bool AllFiniteNonNegative(ReadOnlySpan<Vec3> img)
    {
        foreach (var c in img)
        {
            if (float.IsNaN(c.X) || float.IsNaN(c.Y) || float.IsNaN(c.Z)) return false;
            if (float.IsInfinity(c.X) || float.IsInfinity(c.Y) || float.IsInfinity(c.Z)) return false;
            if (c.X < 0f || c.Y < 0f || c.Z < 0f) return false;
        }
        return true;
    }
}