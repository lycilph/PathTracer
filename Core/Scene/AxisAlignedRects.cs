using Core.Materials;
using Core.Math;

namespace Core.Scene;

// NOTE: For bounding boxes of rectangles we give them a small thickness (epsilon) in the normal axis.
public sealed class XYRect : IHittable
{
    private readonly float _x0, _x1, _y0, _y1, _k;
    private readonly IMaterial _mat;
    private const float Eps = 1e-4f;

    public XYRect(float x0, float x1, float y0, float y1, float k, IMaterial mat)
    { _x0 = x0; _x1 = x1; _y0 = y0; _y1 = y1; _k = k; _mat = mat; }

    public bool Hit(in Ray ray, float tMin, float tMax, out HitRecord hit)
    {
        float t = (_k - ray.Origin.Z) / ray.Direction.Z;
        if (t < tMin || t > tMax) { hit = default; return false; }

        float x = ray.Origin.X + t * ray.Direction.X;
        float y = ray.Origin.Y + t * ray.Direction.Y;
        if (x < _x0 || x > _x1 || y < _y0 || y > _y1) { hit = default; return false; }

        var p = ray.At(t);
        hit = new HitRecord(p, Vec3.UnitZ, t, ray, _mat);
        return true;
    }

    public bool BoundingBox(out Aabb box)
    {
        box = new Aabb(new Vec3(_x0, _y0, _k - Eps), new Vec3(_x1, _y1, _k + Eps));
        return true;
    }
}

public sealed class XZRect : IHittable
{
    private readonly float _x0, _x1, _z0, _z1, _k;
    private readonly IMaterial _mat;
    private const float Eps = 1e-4f;

    public XZRect(float x0, float x1, float z0, float z1, float k, IMaterial mat)
    { _x0 = x0; _x1 = x1; _z0 = z0; _z1 = z1; _k = k; _mat = mat; }

    public bool Hit(in Ray ray, float tMin, float tMax, out HitRecord hit)
    {
        float t = (_k - ray.Origin.Y) / ray.Direction.Y;
        if (t < tMin || t > tMax) { hit = default; return false; }

        float x = ray.Origin.X + t * ray.Direction.X;
        float z = ray.Origin.Z + t * ray.Direction.Z;
        if (x < _x0 || x > _x1 || z < _z0 || z > _z1) { hit = default; return false; }

        var p = ray.At(t);
        hit = new HitRecord(p, Vec3.UnitY, t, ray, _mat);
        return true;
    }

    public bool BoundingBox(out Aabb box)
    {
        box = new Aabb(new Vec3(_x0, _k - Eps, _z0), new Vec3(_x1, _k + Eps, _z1));
        return true;
    }
}

public sealed class YZRect : IHittable
{
    private readonly float _y0, _y1, _z0, _z1, _k;
    private readonly IMaterial _mat;
    private const float Eps = 1e-4f;

    public YZRect(float y0, float y1, float z0, float z1, float k, IMaterial mat)
    { _y0 = y0; _y1 = y1; _z0 = z0; _z1 = z1; _k = k; _mat = mat; }

    public bool Hit(in Ray ray, float tMin, float tMax, out HitRecord hit)
    {
        float t = (_k - ray.Origin.X) / ray.Direction.X;
        if (t < tMin || t > tMax) { hit = default; return false; }

        float y = ray.Origin.Y + t * ray.Direction.Y;
        float z = ray.Origin.Z + t * ray.Direction.Z;
        if (y < _y0 || y > _y1 || z < _z0 || z > _z1) { hit = default; return false; }

        var p = ray.At(t);
        hit = new HitRecord(p, Vec3.UnitX, t, ray, _mat);
        return true;
    }

    public bool BoundingBox(out Aabb box)
    {
        box = new Aabb(new Vec3(_k - Eps, _y0, _z0), new Vec3(_k + Eps, _y1, _z1));
        return true;
    }
}
