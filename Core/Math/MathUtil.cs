using System.Runtime.CompilerServices;

namespace Core.Math;

public static class MathUtil
{
    public const float Pi = 3.14159265358979323846f;
    public const float TwoPi = 2f * Pi;
    public const float InvPi = 1f / Pi;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Clamp(float x, float min, float max) => float.Max(min, float.Min(max, x));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Saturate(float x) => Clamp(x, 0f, 1f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Lerp(float a, float b, float t) => a + (b - a) * t;
}
