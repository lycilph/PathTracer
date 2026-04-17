using Core.Math;

namespace Core.Scene;

public sealed class HittableList : IHittable
{
    private readonly List<IHittable> _objects = new();

    public void Clear() => _objects.Clear();
    public void Add(IHittable obj) => _objects.Add(obj);

    public IReadOnlyList<IHittable> Objects => _objects;

    public bool Hit(in Ray ray, float tMin, float tMax, out HitRecord hit)
    {
        hit = default;
        bool hitAnything = false;
        float closest = tMax;

        foreach (var obj in _objects)
        {
            if (obj.Hit(ray, tMin, closest, out var temp))
            {
                hitAnything = true;
                closest = temp.T;
                hit = temp;
            }
        }

        return hitAnything;
    }

    public bool BoundingBox(out Aabb box)
    {
        if (_objects.Count == 0)
        {
            box = default;
            return false;
        }

        if (!_objects[0].BoundingBox(out box))
            return false;

        for (int i = 1; i < _objects.Count; i++)
        {
            if (!_objects[i].BoundingBox(out var b))
                return false;
            box = Aabb.SurroundingBox(box, b);
        }

        return true;
    }
}