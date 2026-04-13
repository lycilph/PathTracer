using Core.Materials;
using Core.Math;

namespace Core.Scene;

public sealed class Sphere : IHittable
{
    public Vec3 Center { get; }
    public float Radius { get; }
    public IMaterial Material { get; }

    public Sphere(in Vec3 center, float radius, IMaterial material)
    {
        Center = center;
        Radius = radius;
        Material = material;
    }

    public bool Hit(in Ray ray, float tMin, float tMax, out HitRecord hit)
    {
        Vec3 oc = ray.Origin - Center;
        float a = ray.Direction.LengthSquared();
        float halfB = Vec3.Dot(oc, ray.Direction);
        float c = oc.LengthSquared() - Radius * Radius;

        float discriminant = halfB * halfB - a * c;
        if (discriminant < 0f) { hit = default; return false; }

        float sqrtD = float.Sqrt(discriminant);

        float root = (-halfB - sqrtD) / a;
        if (root < tMin || root > tMax)
        {
            root = (-halfB + sqrtD) / a;
            if (root < tMin || root > tMax) { hit = default; return false; }
        }

        Vec3 p = ray.At(root);
        Vec3 outward = (p - Center) / Radius;
        hit = new HitRecord(p, outward, root, ray, Material);
        return true;
    }
}