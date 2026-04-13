using Core.Math;

namespace Core.Scene;

public sealed class Sphere : IHittable
{
    public Vec3 Center { get; }
    public float Radius { get; }

    public Sphere(in Vec3 center, float radius)
    {
        Center = center;
        Radius = radius;
    }

    public bool Hit(in Ray ray, float tMin, float tMax, out HitRecord hit)
    {
        // Solve ||(O + tD) - C||^2 = R^2
        Vec3 oc = ray.Origin - Center;
        float a = ray.Direction.LengthSquared();
        float halfB = Vec3.Dot(oc, ray.Direction);
        float c = oc.LengthSquared() - Radius * Radius;

        float discriminant = halfB * halfB - a * c;
        if (discriminant < 0f)
        {
            hit = default;
            return false;
        }

        float sqrtD = float.Sqrt(discriminant);

        // Find nearest root in range
        float root = (-halfB - sqrtD) / a;
        if (root < tMin || root > tMax)
        {
            root = (-halfB + sqrtD) / a;
            if (root < tMin || root > tMax)
            {
                hit = default;
                return false;
            }
        }

        Vec3 p = ray.At(root);
        Vec3 outwardNormal = (p - Center) / Radius;
        hit = new HitRecord(p, outwardNormal, root, ray);
        return true;
    }
}