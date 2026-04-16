using Core.Math;
using Core.Sampling;

namespace Core.Lights;

/// <summary>
/// Uniformly-sampled rectangular area light aligned with the XZ plane at y=k.
/// </summary>
public sealed class RectAreaLightXZ : ILight
{
    private readonly float _x0, _x1, _z0, _z1, _k;
    private readonly Vec3 _normal; // should point outward from emitting side
    private readonly Vec3 _radiance;
    private readonly float _area;

    public RectAreaLightXZ(float x0, float x1, float z0, float z1, float k, in Vec3 normal, in Vec3 radiance)
    {
        _x0 = x0; _x1 = x1; _z0 = z0; _z1 = z1; _k = k;
        _normal = normal.Normalized();
        _radiance = radiance;
        _area = (_x1 - _x0) * (_z1 - _z0);
    }

    public LightSample Sample(in Vec3 referencePoint, Sampler sampler)
    {
        float u = sampler.Next1D();
        float v = sampler.Next1D();

        float x = MathUtil.Lerp(_x0, _x1, u);
        float z = MathUtil.Lerp(_z0, _z1, v);
        var p = new Vec3(x, _k, z);

        Vec3 d = p - referencePoint;
        float dist2 = d.LengthSquared();
        if (dist2 <= 0f) return default;

        float dist = float.Sqrt(dist2);
        Vec3 wi = d / dist;

        float cosLight = Vec3.Dot(_normal, -wi);
        if (cosLight <= 0f) return default;

        // Uniform area sampling: pdfA = 1/area
        // Convert to solid angle: pdfW = pdfA * dist^2 / cosLight
        float pdf = (dist2 / (cosLight * _area));

        return new LightSample(p, _normal, wi, dist, pdf, _radiance);
    }

    public float Pdf(in Vec3 referencePoint, in Vec3 wi)
    {
        // Intersect ray from referencePoint along wi with plane y = k
        float denom = wi.Y;
        if (float.Abs(denom) < 1e-8f) return 0f;

        float t = (_k - referencePoint.Y) / denom;
        if (t <= 0f) return 0f;

        float x = referencePoint.X + t * wi.X;
        float z = referencePoint.Z + t * wi.Z;
        if (x < _x0 || x > _x1 || z < _z0 || z > _z1) return 0f;

        // Ensure we are on the emitting side
        Vec3 wiN = wi.Normalized();
        float cosLight = Vec3.Dot(_normal, -wiN);
        if (cosLight <= 0f) return 0f;

        // dist^2 = t^2 * |wi|^2. If wi is normalized then dist=t.
        float dist2 = (referencePoint - new Vec3(x, _k, z)).LengthSquared();
        return dist2 / (cosLight * _area);
    }
}
