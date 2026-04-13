using System.Runtime.CompilerServices;

namespace Core.Math;


/// <summary>
/// Immutable ray with optional time parameter (for motion blur).
/// Direction is not automatically normalized (caller decides).
/// </summary>
public readonly struct Ray
{
    public readonly Vec3 Origin;
    public readonly Vec3 Direction;
    public readonly float Time;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Ray(in Vec3 origin, in Vec3 direction, float time = 0f)
        => (Origin, Direction, Time) = (origin, direction, time);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec3 At(float t) => Origin + Direction * t;

    public override string ToString() => $"Ray(O={Origin}, D={Direction}, t={Time})";
}
