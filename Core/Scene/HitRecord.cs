using Core.Math;

namespace Core.Scene;

public readonly struct HitRecord
{
    public readonly Vec3 Point;
    public readonly Vec3 Normal;
    public readonly float T;
    public readonly bool FrontFace;

    public HitRecord(in Vec3 point, in Vec3 outwardNormal, float t, in Ray ray)
    {
        T = t;
        Point = point;
        FrontFace = Vec3.Dot(ray.Direction, outwardNormal) < 0f;
        Normal = FrontFace ? outwardNormal : -outwardNormal;
    }
}
