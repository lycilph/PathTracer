namespace Core;

/// <summary>
/// A half-line defined by an origin and a unit direction (§3.1.2).
/// The valid intersection interval is [TMin, TMax].
/// </summary>
/// <param name="Origin">The starting point of the ray, in world space.</param>
/// <param name="Direction">The unit direction of the ray. Must be normalised by the caller.</param>
/// <param name="TMin">Minimum valid t value. Default 1e-4 avoids self-intersection.</param>
/// <param name="TMax">Maximum valid t value. Default infinity means unbounded.</param>
public readonly record struct Ray(
    Vector3 Origin,
    Vector3 Direction,
    double TMin = 1e-4,
    double TMax = double.PositiveInfinity)
{
    /// <summary>
    /// Evaluates the ray equation P(t) = O + t·D.
    /// </summary>
    /// <param name="t">Distance along the ray. Should be within [TMin, TMax].</param>
    /// <returns>The world-space point at parameter t.</returns>
    public Vector3 At(double t) => Origin + Direction * t;
}