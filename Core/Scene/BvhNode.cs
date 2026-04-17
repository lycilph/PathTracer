using Core.Math;

namespace Core.Scene;

/// <summary>
/// Bounding Volume Hierarchy node.
/// Deterministic build (split by largest axis of centroid bounds).
/// </summary>
public sealed class BvhNode : IHittable
{
    private readonly IHittable _left;
    private readonly IHittable _right;
    private readonly Aabb _box;

    public BvhNode(IReadOnlyList<IHittable> objects)
        : this(objects, 0, objects.Count)
    {
    }

    private BvhNode(IReadOnlyList<IHittable> objects, int start, int end)
    {
        int count = end - start;
        if (count <= 0) throw new ArgumentException("Empty BVH range");

        if (count == 1)
        {
            _left = _right = objects[start];
        }
        else if (count == 2)
        {
            _left = objects[start];
            _right = objects[start + 1];
        }
        else
        {
            // Compute centroid bounds for deterministic axis selection
            var centMin = new Vec3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            var centMax = new Vec3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

            var temp = new List<IHittable>(count);
            for (int i = start; i < end; i++)
            {
                temp.Add(objects[i]);
                if (!objects[i].BoundingBox(out var b))
                    throw new InvalidOperationException("BVH requires bounding boxes");

                var c = (b.Min + b.Max) * 0.5f;
                centMin = Vec3.Min(centMin, c);
                centMax = Vec3.Max(centMax, c);
            }

            Vec3 extent = centMax - centMin;
            int axis = extent.X > extent.Y
                ? (extent.X > extent.Z ? 0 : 2)
                : (extent.Y > extent.Z ? 1 : 2);

            temp.Sort((a, b) =>
            {
                a.BoundingBox(out var ba);
                b.BoundingBox(out var bb);
                float ca = axis == 0 ? (ba.Min.X + ba.Max.X) : axis == 1 ? (ba.Min.Y + ba.Max.Y) : (ba.Min.Z + ba.Max.Z);
                float cb = axis == 0 ? (bb.Min.X + bb.Max.X) : axis == 1 ? (bb.Min.Y + bb.Max.Y) : (bb.Min.Z + bb.Max.Z);
                return ca.CompareTo(cb);
            });

            int mid = count / 2;
            _left = new BvhNode(temp, 0, mid);
            _right = new BvhNode(temp, mid, count);
        }

        if (!_left.BoundingBox(out var bl) || !_right.BoundingBox(out var br))
            throw new InvalidOperationException("BVH children must have bounds");

        _box = Aabb.SurroundingBox(bl, br);
    }

    public bool Hit(in Ray ray, float tMin, float tMax, out HitRecord hit)
    {
        hit = default;
        if (!_box.Hit(ray, tMin, tMax))
            return false;

        bool hitLeft = _left.Hit(ray, tMin, tMax, out var leftHit);
        bool hitRight = _right.Hit(ray, tMin, hitLeft ? leftHit.T : tMax, out var rightHit);

        if (hitRight)
        {
            hit = rightHit;
            return true;
        }

        if (hitLeft)
        {
            hit = leftHit;
            return true;
        }

        return false;
    }

    public bool BoundingBox(out Aabb box)
    {
        box = _box;
        return true;
    }
}