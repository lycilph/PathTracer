using Core.Materials;
using Core.Math;

namespace Core.Scene;

/// <summary>
/// Intersection record.
/// Normal is oriented against incident ray direction.
/// </summary>
public readonly struct HitRecord
{
    public readonly Vec3 Point;
    public readonly Vec3 Normal;
    public readonly float T;
    public readonly bool FrontFace;
    public readonly IMaterial Material;

    public HitRecord(in Vec3 point, in Vec3 outwardNormal, float t, in Ray ray, IMaterial material)
    {
        T = t;
        Point = point;
        FrontFace = Vec3.Dot(ray.Direction, outwardNormal) < 0f;
        Normal = FrontFace ? outwardNormal : -outwardNormal;
        Material = material;
    }

    private HitRecord(in Vec3 point, in Vec3 normal, float t, bool frontFace, IMaterial material)
    {
        Point = point;
        Normal = normal;
        T = t;
        FrontFace = frontFace;
        Material = material;
    }

    public HitRecord Flipped() => new HitRecord(Point, -Normal, T, !FrontFace, Material);
}