using Core.Math;

namespace Core.Scene;

/// <summary>
/// Translates an IHittable by a constant offset.
/// This is a tiny, explicit transform wrapper useful for positioning meshes before full transform support.
/// </summary>
public sealed class Translate : IHittable
{
    private readonly IHittable _obj;
    private readonly Vec3 _offset;

    public Translate(IHittable obj, in Vec3 offset)
    {
        _obj = obj;
        _offset = offset;
    }

    public bool Hit(in Ray ray, float tMin, float tMax, out HitRecord hit)
    {
        // Move ray into the object's local space
        var movedRay = new Ray(ray.Origin - _offset, ray.Direction, ray.Time);
        if (!_obj.Hit(movedRay, tMin, tMax, out hit))
            return false;

        // Move hit point back to world space.
        hit = new HitRecord(hit.Point + _offset, hit.Normal, hit.T, ray, hit.Material);
        return true;
    }

    public bool BoundingBox(out Aabb box)
    {
        if (!_obj.BoundingBox(out box))
            return false;

        box = new Aabb(box.Min + _offset, box.Max + _offset);
        return true;
    }
}