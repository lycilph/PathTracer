using Core.Materials;
using Core.Math;

namespace Core.Scene;

/// <summary>
/// Single triangle (Moller-Trumbore intersection).
/// Stores vertices directly (sufficient for learning; later can be indexed mesh).
/// </summary>
public sealed class Triangle : IHittable
{
    private readonly Vec3 _v0, _v1, _v2;
    private readonly IMaterial _mat;

    public Triangle(in Vec3 v0, in Vec3 v1, in Vec3 v2, IMaterial mat)
    {
        _v0 = v0; _v1 = v1; _v2 = v2; _mat = mat;
    }

    public bool Hit(in Ray ray, float tMin, float tMax, out HitRecord hit)
    {
        // Moller-Trumbore
        Vec3 e1 = _v1 - _v0;
        Vec3 e2 = _v2 - _v0;

        Vec3 pvec = Vec3.Cross(ray.Direction, e2);
        float det = Vec3.Dot(e1, pvec);

        if (float.Abs(det) < 1e-8f)
        {
            hit = default;
            return false;
        }

        float invDet = 1f / det;
        Vec3 tvec = ray.Origin - _v0;
        float u = Vec3.Dot(tvec, pvec) * invDet;
        if (u < 0f || u > 1f)
        {
            hit = default;
            return false;
        }

        Vec3 qvec = Vec3.Cross(tvec, e1);
        float v = Vec3.Dot(ray.Direction, qvec) * invDet;
        if (v < 0f || u + v > 1f)
        {
            hit = default;
            return false;
        }

        float t = Vec3.Dot(e2, qvec) * invDet;
        if (t < tMin || t > tMax)
        {
            hit = default;
            return false;
        }

        Vec3 p = ray.At(t);
        Vec3 outward = Vec3.Cross(e1, e2).Normalized();
        hit = new HitRecord(p, outward, t, ray, _mat);
        return true;
    }

    public bool BoundingBox(out Aabb box)
    {
        var min = Vec3.Min(_v0, Vec3.Min(_v1, _v2));
        var max = Vec3.Max(_v0, Vec3.Max(_v1, _v2));
        // Add epsilon to avoid zero thickness boxes
        const float eps = 1e-4f;
        box = new Aabb(min - new Vec3(eps, eps, eps), max + new Vec3(eps, eps, eps));
        return true;
    }
}