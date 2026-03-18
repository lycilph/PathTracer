namespace Core;

/// <summary>
/// A parallelogram primitive defined by a corner and two edge vectors (§3.3.2).
/// Used for Cornell Box walls and area lights.
/// </summary>
/// <param name="Corner">One corner of the quad in world space.</param>
/// <param name="Edge1">First edge vector (world units). Not required to be unit length.</param>
/// <param name="Edge2">Second edge vector (world units). Not required to be unit length.</param>
/// <param name="Material">The material the quad will be rendered with.</param>
public sealed class Quad(Vector3 Corner, Vector3 Edge1, Vector3 Edge2, IMaterial Material) : IHittable, IBoundable
{
    public Vector3 Corner { get; } = Corner;
    public Vector3 Edge1 { get; } = Edge1;
    public Vector3 Edge2 { get; } = Edge2;
    public IMaterial Material { get; } = Material;

    /// <summary>Outward-facing unit normal, computed once at construction.</summary>
    public Vector3 Normal { get; } = Vector3.Cross(Edge1, Edge2).Normalize();

    /// <inheritdoc/>
    /// <remarks>
    /// Uses the Möller-Trumbore algorithm adapted for a parallelogram:
    /// u and v each independently in [0,1] (no u+v ≤ 1 constraint).
    /// </remarks>
    public bool Hit(Ray ray, out HitRecord hit)
    {
        hit = default;

        var h = Vector3.Cross(ray.Direction, Edge2);
        var a = Vector3.Dot(Edge1, h);

        // Ray is parallel to the quad plane
        if (Math.Abs(a) < 1e-10)
            return false;

        var f = 1.0 / a;
        var s = ray.Origin - Corner;
        var u = f * Vector3.Dot(s, h);

        if (u < 0 || u > 1)
            return false;

        var q = Vector3.Cross(s, Edge1);
        var v = f * Vector3.Dot(ray.Direction, q);

        if (v < 0 || v > 1)
            return false;

        var t = f * Vector3.Dot(Edge2, q);

        if (t < ray.TMin || t > ray.TMax)
            return false;

        var point = ray.At(t);
        hit = HitRecord.Create(t, point, ray, Normal, Material);
        return true;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Computes the AABB from all four corners, then pads by 1e-4 on each
    /// axis to avoid zero-thickness boxes for axis-aligned quads.
    /// </remarks>
    public Aabb GetBounds()
    {
        const double pad = 1e-4;

        // All four corners of the parallelogram
        var c0 = Corner;
        var c1 = Corner + Edge1;
        var c2 = Corner + Edge2;
        var c3 = Corner + Edge1 + Edge2;

        var min = new Vector3(
            Math.Min(Math.Min(c0.X, c1.X), Math.Min(c2.X, c3.X)) - pad,
            Math.Min(Math.Min(c0.Y, c1.Y), Math.Min(c2.Y, c3.Y)) - pad,
            Math.Min(Math.Min(c0.Z, c1.Z), Math.Min(c2.Z, c3.Z)) - pad);

        var max = new Vector3(
            Math.Max(Math.Max(c0.X, c1.X), Math.Max(c2.X, c3.X)) + pad,
            Math.Max(Math.Max(c0.Y, c1.Y), Math.Max(c2.Y, c3.Y)) + pad,
            Math.Max(Math.Max(c0.Z, c1.Z), Math.Max(c2.Z, c3.Z)) + pad);

        return new Aabb(min, max);
    }
}