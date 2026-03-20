using Core.Algebra;

namespace Core.Geometry;

/// <summary>
/// Contains all information about a ray-surface intersection (§3.3).
/// The stored Normal always points against the incident ray direction.
/// </summary>
public readonly record struct HitRecord
{
    /// <summary>Ray parameter t at the intersection. P = ray.At(T).</summary>
    public double T { get; init; }

    /// <summary>World-space position of the intersection.</summary>
    public Vector3 Point { get; init; }

    /// <summary>
    /// Surface normal at the intersection, always pointing against the incident ray.
    /// Use FrontFace to determine if the ray hit the outside or inside of the surface.
    /// </summary>
    public Vector3 Normal { get; init; }

    /// <summary>
    /// True if the ray hit the front (outside) face of the surface.
    /// False if the ray was travelling inside the surface.
    /// </summary>
    public bool FrontFace { get; init; }
    
    /// <summary>The material of the surface that was hit.</summary>
    public IMaterial Material { get; init; }

    /// <summary>
    /// Constructs a HitRecord, automatically orienting the normal against the ray.
    /// </summary>
    /// <param name="t">The ray parameter at the hit.</param>
    /// <param name="point">The world-space hit position.</param>
    /// <param name="ray">The incident ray.</param>
    /// <param name="outwardNormal">
    /// The geometric outward-facing normal of the surface. Must be unit length.
    /// </param>
    public static HitRecord Create(double t, Vector3 point, Ray ray, Vector3 outwardNormal, IMaterial material)
    {
        // If the ray and outward normal point in the same direction,
        // the ray is hitting the inside face.
        var frontFace = Vector3.Dot(ray.Direction, outwardNormal) < 0;
        return new HitRecord
        {
            T = t,
            Point = point,
            FrontFace = frontFace,
            // Flip the normal so it always opposes the ray direction
            Normal = frontFace ? outwardNormal : -outwardNormal,
            Material = material
        };
    }
}