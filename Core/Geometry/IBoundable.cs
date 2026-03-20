using Core.Acceleration;

namespace Core.Geometry;

/// <summary>
/// Implemented by any primitive that can report an axis-aligned bounding box.
/// Required for BVH construction (§3.4).
/// </summary>
public interface IBoundable
{
    /// <summary>
    /// Returns the tight axis-aligned bounding box enclosing this primitive.
    /// </summary>
    Aabb GetBounds();
}