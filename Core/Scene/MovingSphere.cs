using Core.Materials;
using Core.Math;

namespace Core.Scene;

// <summary>
/// A sphere with linearly moving center over time.
/// BoundingBox encloses the entire motion range.
/// </summary>
public sealed class MovingSphere : IHittable
{
    private readonly Vec3 _center0;
    private readonly Vec3 _center1;
    private readonly float _time0;
    private readonly float _time1;

    public float Radius { get; }
    public IMaterial Material { get; }

    public MovingSphere(in Vec3 center0, in Vec3 center1, float time0, float time1, float radius, IMaterial material)
    {
        _center0 = center0;
        _center1 = center1;
        _time0 = time0;
        _time1 = time1;
        Radius = radius;
        Material = material;
    }

    public Vec3 Center(float time)
    {
        if (_time1 <= _time0) return _center0;
        float t = (time - _time0) / (_time1 - _time0);
        t = MathUtil.Clamp(t, 0f, 1f);
        return _center0 + (_center1 - _center0) * t;
    }

    public bool Hit(in Ray ray, float tMin, float tMax, out HitRecord hit)
    {
        Vec3 c = Center(ray.Time);

        Vec3 oc = ray.Origin - c;
        float a = ray.Direction.LengthSquared();
        float halfB = Vec3.Dot(oc, ray.Direction);
        float cc = oc.LengthSquared() - Radius * Radius;

        float discriminant = halfB * halfB - a * cc;
        if (discriminant < 0f) { hit = default; return false; }

        float sqrtD = float.Sqrt(discriminant);

        float root = (-halfB - sqrtD) / a;
        if (root < tMin || root > tMax)
        {
            root = (-halfB + sqrtD) / a;
            if (root < tMin || root > tMax) { hit = default; return false; }
        }

        Vec3 p = ray.At(root);
        Vec3 outward = (p - c) / Radius;
        hit = new HitRecord(p, outward, root, ray, Material);
        return true;
    }

    public bool BoundingBox(out Aabb box)
    {
        var r = new Vec3(Radius, Radius, Radius);

        var b0 = new Aabb(_center0 - r, _center0 + r);
        var b1 = new Aabb(_center1 - r, _center1 + r);

        box = Aabb.SurroundingBox(b0, b1);
        return true;
    }
}