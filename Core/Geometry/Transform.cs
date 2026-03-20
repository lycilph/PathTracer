using Core.Acceleration;
using Core.Algebra;

namespace Core.Geometry;

/// <summary>
/// Wraps an IHittable with a world-space transformation (§3.1.1).
/// Rays are transformed into object space for intersection testing,
/// and hit normals are transformed back into world space.
/// </summary>
public sealed class Transform : IHittable, IBoundable
{
    private readonly IHittable _inner;
    private readonly Matrix4x4d _worldToObject; // inverse of the TRS matrix
    private readonly Matrix4x4d _objectToWorld;
    private readonly Aabb _worldBounds;

    /// <param name="inner">The primitive to transform.</param>
    /// <param name="objectToWorld">
    /// The transform matrix taking object-space points to world space.
    /// Build using Matrix4x4d.Translation/Scale/RotationY etc. and multiply.
    /// </param>
    public Transform(IHittable inner, Matrix4x4d objectToWorld)
    {
        _inner = inner;
        _objectToWorld = objectToWorld;
        _worldToObject = objectToWorld.Inverse();
        _worldBounds = TransformBounds(inner, objectToWorld);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Transforms the ray into object space, tests intersection, then
    /// transforms the hit point and normal back into world space.
    /// </remarks>
    public bool Hit(Ray ray, out HitRecord hit)
    {
        hit = default;

        // Transform ray from world space to object space
        var localOrigin = _worldToObject.TransformPoint(ray.Origin);
        var localDirection = _worldToObject.TransformDirection(ray.Direction);

        // Preserve TMin/TMax — t values are invariant under rigid transforms
        // (non-uniform scale would change distances, but we accept this)
        var localRay = new Ray(localOrigin, localDirection, ray.TMin, ray.TMax);

        if (!_inner.Hit(localRay, out var localHit))
            return false;

        // Transform hit point and normal back to world space
        var worldPoint = _objectToWorld.TransformPoint(localHit.Point);
        var worldNormal = _worldToObject.TransformNormal(localHit.Normal);

        // Reconstruct the hit record with world-space geometry
        // We pass the world-space outward normal directly
        var outwardNormal = localHit.FrontFace ? worldNormal : -worldNormal;
        hit = HitRecord.Create(localHit.T, worldPoint, ray, outwardNormal,
                               localHit.Material);
        return true;
    }

    /// <inheritdoc/>
    public Aabb GetBounds() => _worldBounds;

    /// <summary>
    /// Computes a world-space AABB by transforming all 8 corners of the
    /// object-space AABB and taking their union.
    /// </summary>
    private static Aabb TransformBounds(IHittable inner, Matrix4x4d m)
    {
        if (inner is not IBoundable boundable)
            return new Aabb(Vector3.Zero, Vector3.Zero);

        var b = boundable.GetBounds();

        // Transform all 8 corners of the AABB
        var corners = new[]
        {
            m.TransformPoint(new Vector3(b.Min.X, b.Min.Y, b.Min.Z)),
            m.TransformPoint(new Vector3(b.Max.X, b.Min.Y, b.Min.Z)),
            m.TransformPoint(new Vector3(b.Min.X, b.Max.Y, b.Min.Z)),
            m.TransformPoint(new Vector3(b.Max.X, b.Max.Y, b.Min.Z)),
            m.TransformPoint(new Vector3(b.Min.X, b.Min.Y, b.Max.Z)),
            m.TransformPoint(new Vector3(b.Max.X, b.Min.Y, b.Max.Z)),
            m.TransformPoint(new Vector3(b.Min.X, b.Max.Y, b.Max.Z)),
            m.TransformPoint(new Vector3(b.Max.X, b.Max.Y, b.Max.Z)),
        };

        var min = corners[0];
        var max = corners[0];
        foreach (var c in corners.Skip(1))
        {
            min = new Vector3(
                Math.Min(min.X, c.X),
                Math.Min(min.Y, c.Y),
                Math.Min(min.Z, c.Z));
            max = new Vector3(
                Math.Max(max.X, c.X),
                Math.Max(max.Y, c.Y),
                Math.Max(max.Z, c.Z));
        }

        return new Aabb(min, max);
    }
}