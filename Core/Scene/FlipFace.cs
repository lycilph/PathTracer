using Core.Math;

namespace Core.Scene;

/// <summary>
/// Wraps a hittable and flips the reported normal orientation.
/// Useful for one-sided lights and for building closed shapes.
/// </summary>
public sealed class FlipFace : IHittable
{
    private readonly IHittable _obj;

    public FlipFace(IHittable obj) => _obj = obj;

    public bool Hit(in Ray ray, float tMin, float tMax, out HitRecord hit)
    {
        if (!_obj.Hit(ray, tMin, tMax, out hit))
            return false;

        hit = hit.Flipped();
        return true;
    }

    public bool BoundingBox(out Aabb box) => _obj.BoundingBox(out box);
}
