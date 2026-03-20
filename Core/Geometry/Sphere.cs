using Core.Acceleration;
using Core.Algebra;

namespace Core.Geometry;

/// <summary>
/// A geometric sphere primitive (§3.3.1).
/// </summary>
/// <param name="Centre">Centre of the sphere in world space.</param>
/// <param name="Radius">Radius of the sphere in world units.</param>
/// <param name="Material">The material the sphere will be rendered with.</param>
public sealed class Sphere(Vector3 Centre, double Radius, IMaterial Material) : IHittable, IBoundable
{
    public Vector3 Centre { get; } = Centre;
    public double Radius { get; } = Radius;
    public IMaterial Material { get; } = Material;

    /// <inheritdoc/>
    /// <remarks>
    /// Solves (D·D)t² + 2(oc·D)t + (oc·oc − r²) = 0 using the half-b
    /// substitution to avoid unnecessary multiplications by 2.
    /// </remarks>
    public bool Hit(Ray ray, out HitRecord hit)
    {
        hit = default;

        var oc = ray.Origin - Centre;

        var a = ray.Direction.LengthSquared;
        var h = Vector3.Dot(oc, ray.Direction);   // half-b
        var c = oc.LengthSquared - Radius * Radius;

        var discriminant = h * h - a * c;

        if (discriminant < 0)
            return false;

        var sqrtD = Math.Sqrt(discriminant);

        // Find the nearest root within [TMin, TMax]
        var t = (-h - sqrtD) / a;
        if (t < ray.TMin || t > ray.TMax)
        {
            t = (-h + sqrtD) / a;
            if (t < ray.TMin || t > ray.TMax)
                return false;
        }

        var point = ray.At(t);
        var outwardNormal = (point - Centre) / Radius;  // already unit length

        hit = HitRecord.Create(t, point, ray, outwardNormal, Material);
        return true;
    }

    /// <inheritdoc/>
    /// <remarks>Tight AABB: centre ± radius on each axis.</remarks>
    public Aabb GetBounds() => new(
        Centre - new Vector3(Radius, Radius, Radius),
        Centre + new Vector3(Radius, Radius, Radius));
}