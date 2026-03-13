namespace Core;

/// <summary>
/// Implemented by any geometric primitive that a ray can intersect.
/// </summary>
public interface IHittable
{
    /// <summary>
    /// Tests whether <paramref name="ray"/> intersects this object within
    /// its valid interval [ray.TMin, ray.TMax].
    /// </summary>
    /// <param name="ray">The incident ray.</param>
    /// <param name="hit">
    /// Populated with intersection data if a hit is found; undefined otherwise.
    /// </param>
    /// <returns>True if an intersection was found within the valid interval.</returns>
    bool Hit(Ray ray, out HitRecord hit);
}