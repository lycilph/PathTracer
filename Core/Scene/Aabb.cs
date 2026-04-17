using Core.Math;

namespace Core.Scene;

/// <summary>
/// Axis-aligned bounding box.
/// Used for BVH acceleration.
/// </summary>
public readonly struct Aabb
{
    public readonly Vec3 Min;
    public readonly Vec3 Max;

    public Aabb(in Vec3 min, in Vec3 max)
        => (Min, Max) = (min, max);

    public static Aabb SurroundingBox(in Aabb a, in Aabb b)
        => new(Vec3.Min(a.Min, b.Min), Vec3.Max(a.Max, b.Max));

    public bool Hit(in Ray ray, float tMin, float tMax)
    {
        // Slab method
        for (int axis = 0; axis < 3; axis++)
        {
            float origin = axis == 0 ? ray.Origin.X : axis == 1 ? ray.Origin.Y : ray.Origin.Z;
            float dir = axis == 0 ? ray.Direction.X : axis == 1 ? ray.Direction.Y : ray.Direction.Z;
            float invD = 1.0f / dir;

            float minA = axis == 0 ? Min.X : axis == 1 ? Min.Y : Min.Z;
            float maxA = axis == 0 ? Max.X : axis == 1 ? Max.Y : Max.Z;

            float t0 = (minA - origin) * invD;
            float t1 = (maxA - origin) * invD;
            if (invD < 0f) (t0, t1) = (t1, t0);

            tMin = t0 > tMin ? t0 : tMin;
            tMax = t1 < tMax ? t1 : tMax;

            if (tMax <= tMin)
                return false;
        }

        return true;
    }
}