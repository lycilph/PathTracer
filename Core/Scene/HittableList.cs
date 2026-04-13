using Core.Math;

namespace Core.Scene;

public sealed class HittableList : IHittable
{
    private readonly List<IHittable> _objects = new();

    public void Clear() => _objects.Clear();
    public void Add(IHittable obj) => _objects.Add(obj);

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
}