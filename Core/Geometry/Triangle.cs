using Core.Acceleration;
using Core.Algebra;

namespace Core.Geometry;

/// <summary>
/// A triangle primitive using the Möller-Trumbore intersection algorithm (§3.3.2).
/// Supports optional per-vertex normal interpolation for smooth shading.
/// </summary>
public sealed class Triangle : IHittable, IBoundable
{
    private readonly Vector3 _v0, _v1, _v2;
    private readonly Vector3 _e1, _e2;         // precomputed edges
    private readonly Vector3 _flatNormal;       // geometric normal
    private readonly Vector3? _n0, _n1, _n2;    // optional vertex normals

    /// <summary>The material applied to this triangle.</summary>
    public IMaterial Material { get; }

    /// <param name="v0">First vertex in world space.</param>
    /// <param name="v1">Second vertex in world space.</param>
    /// <param name="v2">Third vertex in world space.</param>
    /// <param name="material">Surface material.</param>
    /// <param name="n0">Optional vertex normal at v0. Must be unit length.</param>
    /// <param name="n1">Optional vertex normal at v1. Must be unit length.</param>
    /// <param name="n2">Optional vertex normal at v2. Must be unit length.</param>
    public Triangle(
        Vector3 v0, Vector3 v1, Vector3 v2,
        IMaterial material,
        Vector3? n0 = null, Vector3? n1 = null, Vector3? n2 = null)
    {
        _v0 = v0; _v1 = v1; _v2 = v2;
        _e1 = v1 - v0;
        _e2 = v2 - v0;
        _flatNormal = Vector3.Cross(_e1, _e2).Normalize();
        Material = material;
        _n0 = n0; _n1 = n1; _n2 = n2;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Möller-Trumbore algorithm (§3.3.2).
    /// Returns the nearest hit within [ray.TMin, ray.TMax].
    /// If vertex normals are present, the hit normal is interpolated
    /// using barycentric coordinates for smooth shading.
    /// </remarks>
    public bool Hit(Ray ray, out HitRecord hit)
    {
        hit = default;

        var h = Vector3.Cross(ray.Direction, _e2);
        var a = Vector3.Dot(_e1, h);

        // Ray is parallel to the triangle plane
        if (Math.Abs(a) < 1e-10)
            return false;

        var f = 1.0 / a;
        var s = ray.Origin - _v0;
        var u = f * Vector3.Dot(s, h);

        if (u < 0 || u > 1)
            return false;

        var q = Vector3.Cross(s, _e1);
        var v = f * Vector3.Dot(ray.Direction, q);

        // Triangle constraint: u + v ≤ 1 (quad allows each independently)
        if (v < 0 || u + v > 1)
            return false;

        var t = f * Vector3.Dot(_e2, q);
        if (t < ray.TMin || t > ray.TMax)
            return false;

        var point = ray.At(t);
        var normal = InterpolateNormal(u, v);
        hit = HitRecord.Create(t, point, ray, normal, Material);
        return true;
    }

    /// <inheritdoc/>
    /// <remarks>
    /// AABB is the min/max of all three vertices with a small epsilon
    /// padding to avoid zero-thickness boxes for axis-aligned triangles.
    /// </remarks>
    public Aabb GetBounds()
    {
        const double pad = 1e-4;
        return new Aabb(
            new Vector3(
                Math.Min(_v0.X, Math.Min(_v1.X, _v2.X)) - pad,
                Math.Min(_v0.Y, Math.Min(_v1.Y, _v2.Y)) - pad,
                Math.Min(_v0.Z, Math.Min(_v1.Z, _v2.Z)) - pad),
            new Vector3(
                Math.Max(_v0.X, Math.Max(_v1.X, _v2.X)) + pad,
                Math.Max(_v0.Y, Math.Max(_v1.Y, _v2.Y)) + pad,
                Math.Max(_v0.Z, Math.Max(_v1.Z, _v2.Z)) + pad));
    }

    /// <summary>
    /// Returns the surface normal at barycentric coordinates (u, v).
    /// Uses interpolated vertex normals if available, otherwise flat normal.
    /// </summary>
    /// <remarks>
    /// Barycentric interpolation: N = (1−u−v)·N0 + u·N1 + v·N2 (§5 mesh notes).
    /// </remarks>
    private Vector3 InterpolateNormal(double u, double v)
    {
        if (_n0 is null || _n1 is null || _n2 is null)
            return _flatNormal;

        var w = 1.0 - u - v;
        return (w * _n0.Value + u * _n1.Value + v * _n2.Value).Normalize();
    }
}