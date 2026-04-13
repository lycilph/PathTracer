using System.Runtime.CompilerServices;

namespace Core.Math;


/// <summary>
/// Minimal immutable 3D vector for rendering math.
/// Uses float for performance and sufficient precision in typical rendering workflows.
/// </summary>
public readonly struct Vec3 : IEquatable<Vec3>
{
    public readonly float X;
    public readonly float Y;
    public readonly float Z;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec3(float x, float y, float z) => (X, Y, Z) = (x, y, z);

    public static Vec3 Zero => new(0f, 0f, 0f);
    public static Vec3 One => new(1f, 1f, 1f);

    public static Vec3 UnitX => new(1f, 0f, 0f);
    public static Vec3 UnitY => new(0f, 1f, 0f);
    public static Vec3 UnitZ => new(0f, 0f, 1f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float LengthSquared() => X * X + Y * Y + Z * Z;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float Length() => float.Sqrt(LengthSquared());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec3 Normalized()
    {
        float len = Length();
        return len > 0f ? this / len : Zero;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool NearZero(float eps = 1e-8f) =>
        float.Abs(X) < eps && float.Abs(Y) < eps && float.Abs(Z) < eps;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Dot(in Vec3 a, in Vec3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec3 Cross(in Vec3 a, in Vec3 b) =>
        new(a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec3 Hadamard(in Vec3 a, in Vec3 b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec3 Min(in Vec3 a, in Vec3 b) =>
        new(float.Min(a.X, b.X), float.Min(a.Y, b.Y), float.Min(a.Z, b.Z));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec3 Max(in Vec3 a, in Vec3 b) =>
        new(float.Max(a.X, b.X), float.Max(a.Y, b.Y), float.Max(a.Z, b.Z));

    // Operators
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec3 operator +(in Vec3 a, in Vec3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec3 operator -(in Vec3 a, in Vec3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec3 operator -(in Vec3 v) => new(-v.X, -v.Y, -v.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec3 operator *(in Vec3 v, float s) => new(v.X * s, v.Y * s, v.Z * s);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec3 operator *(float s, in Vec3 v) => v * s;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vec3 operator /(in Vec3 v, float s) => new(v.X / s, v.Y / s, v.Z / s);

    // Equality — exact float equality (use approximate comparisons in tests when needed)
    public bool Equals(Vec3 other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
    public override bool Equals(object? obj) => obj is Vec3 v && Equals(v);
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);

    public static bool operator ==(Vec3 left, Vec3 right) => left.Equals(right);
    public static bool operator !=(Vec3 left, Vec3 right) => !left.Equals(right);

    public override string ToString() => $"Vec3({X}, {Y}, {Z})";
}
