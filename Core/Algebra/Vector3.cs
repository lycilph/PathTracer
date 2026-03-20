namespace Core.Algebra;

/// <summary>
/// An immutable 3-component vector used for both positions and directions.
/// All components are double-precision to avoid floating-point artefacts (§3.1.1).
/// </summary>
/// <param name="X">X component, in world units.</param>
/// <param name="Y">Y component, in world units.</param>
/// <param name="Z">Z component, in world units.</param>
public readonly record struct Vector3(double X, double Y, double Z)
{
    public static readonly Vector3 Zero = new(0, 0, 0);
    public static readonly Vector3 One = new(1, 1, 1);
    public static readonly Vector3 UnitX = new(1, 0, 0);
    public static readonly Vector3 UnitY = new(0, 1, 0);
    public static readonly Vector3 UnitZ = new(0, 0, 1);

    // ── Arithmetic ────────────────────────────────────────────────────────────

    public static Vector3 operator +(Vector3 a, Vector3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    public static Vector3 operator -(Vector3 a, Vector3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    public static Vector3 operator -(Vector3 v) => new(-v.X, -v.Y, -v.Z);
    public static Vector3 operator *(Vector3 a, Vector3 b) => new(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
    public static Vector3 operator *(Vector3 v, double s) => new(v.X * s, v.Y * s, v.Z * s);
    public static Vector3 operator *(double s, Vector3 v) => v * s;
    public static Vector3 operator /(Vector3 v, double s) => new(v.X / s, v.Y / s, v.Z / s);

    // ── Fundamental operations ────────────────────────────────────────────────

    /// <summary>
    /// Dot product: a·b = axbx + ayby + azbz.
    /// Returns a scalar measuring how aligned two vectors are.
    /// </summary>
    public static double Dot(Vector3 a, Vector3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

    /// <summary>
    /// Cross product: a×b, perpendicular to both a and b (right-hand rule).
    /// </summary>
    public static Vector3 Cross(Vector3 a, Vector3 b) => new(
        a.Y * b.Z - a.Z * b.Y,
        a.Z * b.X - a.X * b.Z,
        a.X * b.Y - a.Y * b.X);

    /// <summary>Squared length ‖v‖² — cheaper than Length when only comparisons are needed.</summary>
    public double LengthSquared => Dot(this, this);

    /// <summary>Euclidean length ‖v‖ = √(x²+y²+z²).</summary>
    public double Length => Math.Sqrt(LengthSquared);

    /// <summary>
    /// Returns the unit vector v̂ = v/‖v‖.
    /// </summary>
    /// <remarks>Caller is responsible for ensuring v is not the zero vector.</remarks>
    public Vector3 Normalize() => this / Length;

    /// <summary>Returns true if all components are within <paramref name="epsilon"/> of zero.</summary>
    public bool IsNearZero(double epsilon = 1e-10)
        => Math.Abs(X) < epsilon && Math.Abs(Y) < epsilon && Math.Abs(Z) < epsilon;

    public override string ToString() => $"({X:F4}, {Y:F4}, {Z:F4})";
}