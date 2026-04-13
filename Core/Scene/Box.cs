using Core.Materials;
using Core.Math;

namespace Core.Scene;

/// <summary>
/// Axis-aligned box made of 6 rectangles.
/// </summary>
public sealed class Box : IHittable
{
    private readonly HittableList _sides = new();

    public Box(in Vec3 p0, in Vec3 p1, IMaterial mat)
    {
        _sides.Add(new XYRect(p0.X, p1.X, p0.Y, p1.Y, p1.Z, mat));
        _sides.Add(new FlipFace(new XYRect(p0.X, p1.X, p0.Y, p1.Y, p0.Z, mat)));

        _sides.Add(new XZRect(p0.X, p1.X, p0.Z, p1.Z, p1.Y, mat));
        _sides.Add(new FlipFace(new XZRect(p0.X, p1.X, p0.Z, p1.Z, p0.Y, mat)));

        _sides.Add(new YZRect(p0.Y, p1.Y, p0.Z, p1.Z, p1.X, mat));
        _sides.Add(new FlipFace(new YZRect(p0.Y, p1.Y, p0.Z, p1.Z, p0.X, mat)));
    }

    public bool Hit(in Ray ray, float tMin, float tMax, out HitRecord hit)
        => _sides.Hit(ray, tMin, tMax, out hit);
}