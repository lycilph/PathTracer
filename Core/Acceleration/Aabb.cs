using Core.Algebra;

namespace Core.Acceleration;

/// <summary>
/// Axis-Aligned Bounding Box used for BVH traversal (§3.3.3).
/// </summary>
/// <param name="Min">The minimum corner of the box in world space.</param>
/// <param name="Max">The maximum corner of the box in world space.</param>
public readonly record struct Aabb(Vector3 Min, Vector3 Max)
{
    /// <summary>
    /// Tests whether <paramref name="ray"/> intersects this AABB within
    /// the ray's valid interval using the slab method (§3.3.3).
    /// </summary>
    public bool Hit(Ray ray)
    {
        // Test each axis slab and accumulate the overlap interval
        var tNear = ray.TMin;
        var tFar = ray.TMax;

        for (var axis = 0; axis < 3; axis++)
        {
            var origin = GetComponent(ray.Origin, axis);
            var direction = GetComponent(ray.Direction, axis);
            var bMin = GetComponent(Min, axis);
            var bMax = GetComponent(Max, axis);

            // Avoid division by zero for rays parallel to this axis slab
            if (Math.Abs(direction) < 1e-10)
            {
                // Ray is parallel — if origin is outside the slab, no hit
                if (origin < bMin || origin > bMax)
                    return false;
                continue;
            }

            var t0 = (bMin - origin) / direction;
            var t1 = (bMax - origin) / direction;

            if (t0 > t1) (t0, t1) = (t1, t0); // ensure t0 is the entry

            tNear = Math.Max(tNear, t0);
            tFar = Math.Min(tFar, t1);

            if (tNear > tFar)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Returns the smallest AABB that contains both this box and <paramref name="other"/>.
    /// Used during BVH construction to compute parent node bounds.
    /// </summary>
    public Aabb ExpandTo(Aabb other) => new(
        new Vector3(
            Math.Min(Min.X, other.Min.X),
            Math.Min(Min.Y, other.Min.Y),
            Math.Min(Min.Z, other.Min.Z)),
        new Vector3(
            Math.Max(Max.X, other.Max.X),
            Math.Max(Max.Y, other.Max.Y),
            Math.Max(Max.Z, other.Max.Z)));

    /// <summary>Returns the centroid of the box — used for BVH split decisions.</summary>
    public Vector3 Centroid => (Min + Max) * 0.5;

    /// <summary>Returns the surface area of the box — used for SAH cost in M2.</summary>
    public double SurfaceArea
    {
        get
        {
            var e = Max - Min;
            return 2.0 * (e.X * e.Y + e.Y * e.Z + e.Z * e.X);
        }
    }

    private static double GetComponent(Vector3 v, int axis) => axis switch
    {
        0 => v.X,
        1 => v.Y,
        _ => v.Z
    };
}