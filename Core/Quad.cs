namespace Core;

/// <summary>
/// A parallelogram primitive defined by a corner and two edge vectors (§3.3.2).
/// Used for Cornell Box walls and area lights.
/// </summary>
/// <param name="Corner">One corner of the quad in world space.</param>
/// <param name="Edge1">First edge vector (world units). Not required to be unit length.</param>
/// <param name="Edge2">Second edge vector (world units). Not required to be unit length.</param>
/// <param name="Material">The material the quad will be rendered with.</param>
public sealed class Quad(Vector3 Corner, Vector3 Edge1, Vector3 Edge2, IMaterial Material) : IHittable
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
}