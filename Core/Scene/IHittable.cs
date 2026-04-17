using Core.Math;

namespace Core.Scene;

public interface IHittable
{
    bool Hit(in Ray ray, float tMin, float tMax, out HitRecord hit);

    bool BoundingBox(out Aabb box);
}