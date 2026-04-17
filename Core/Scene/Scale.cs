using Core.Math;

namespace Core.Scene;

/// <summary>
/// Uniformly scales an IHittable around the origin.
/// 
/// NOTE: This is a deliberately small learning wrapper. It assumes uniform scale.
/// For non-uniform scale, normals must be transformed differently.
/// 
/// For uniform scale s:
/// - Object space ray origin = O / s
/// - Object space ray direction = D / s
/// - Hit distance t stays the same (because r(t) = O + tD).
/// - Hit point = P_local * s
/// - Normal unchanged for uniform scale (after normalization).
/// </summary>
public sealed class Scale : IHittable
{
    private readonly IHittable _obj;
    private readonly float _s;
    private readonly float _invS;

    public Scale(IHittable obj, float s)
    {
        _obj = obj;
        _s = s;
        _invS = 1f / s;
    }

    public bool Hit(in Ray ray, float tMin, float tMax, out HitRecord hit)
    {
        // Transform ray to local space
        var o = ray.Origin * _invS;
        var d = ray.Direction * _invS;
        var scaledRay = new Ray(o, d, ray.Time);

        if (!_obj.Hit(scaledRay, tMin, tMax, out hit))
            return false;

        // Transform hit back
        var pWorld = hit.Point * _s;
        var nWorld = hit.Normal; // uniform scale -> normal direction preserved

        hit = new HitRecord(pWorld, nWorld, hit.T, ray, hit.Material);
        return true;
    }

    public bool BoundingBox(out Aabb box)
    {
        if (!_obj.BoundingBox(out box))
            return false;

        // Uniform scale around origin
        var min = box.Min * _s;
        var max = box.Max * _s;
        // Handle negative scales by sorting min/max
        box = new Aabb(Vec3.Min(min, max), Vec3.Max(min, max));
        return true;
    }
}