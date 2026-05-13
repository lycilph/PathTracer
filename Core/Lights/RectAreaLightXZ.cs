using Core.Math;
using Core.Sampling;

namespace Core.Lights;

/// <summary>
/// Uniformly-sampled rectangular area light aligned with the XZ plane at y=k.
/// Implements IPhotonEmitter for use in SPPM photon passes.
/// </summary>
public sealed class RectAreaLightXZ : ILight, IPhotonEmitter
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

    // ── IPhotonEmitter ────────────────────────────────────────────────────────

    /// <summary>
    /// Emits a photon from a uniformly-sampled point on the light surface in a
    /// cosine-weighted direction aligned with the light normal.
    ///
    /// Power = Radiance × π × Area  (total Lambertian-emitter flux).
    /// The SPPM renderer divides by total_photons_emitted in the final estimate,
    /// so this value is independent of the photon budget M.
    /// </summary>
    public PhotonEmission EmitPhoton(Sampler sampler)
    {
        // Uniform position on light rectangle
        float x = MathUtil.Lerp(_x0, _x1, sampler.Next1D());
        float z = MathUtil.Lerp(_z0, _z1, sampler.Next1D());
        var position = new Vec3(x, _k, z);

        // Cosine-weighted direction about the emission normal
        Vec3 direction = SampleCosineHemisphere(sampler, _normal);

        // Total Lambertian emitted power: L_e × π × A
        Vec3 power = _radiance * (MathUtil.Pi * _area);

        return new PhotonEmission(position, direction, power);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Vec3 SampleCosineHemisphere(Sampler sampler, in Vec3 normal)
    {
        float u1 = sampler.Next1D();
        float u2 = sampler.Next1D();

        // Malley's method: project concentric-disk sample onto hemisphere
        float r = float.Sqrt(u1);
        float theta = MathUtil.TwoPi * u2;
        float lx = r * float.Cos(theta);
        float ly = r * float.Sin(theta);
        float lz = float.Sqrt(float.Max(0f, 1f - u1));

        var local = new Vec3(lx, ly, lz);
        return ToWorld(local, normal);
    }

    private static Vec3 ToWorld(in Vec3 local, in Vec3 n)
    {
        Vec3 w = n.Normalized();
        Vec3 a = float.Abs(w.X) > 0.9f ? Vec3.UnitY : Vec3.UnitX;
        Vec3 v = Vec3.Cross(w, a).Normalized();
        Vec3 u = Vec3.Cross(v, w);
        return (u * local.X + v * local.Y + w * local.Z).Normalized();
    }
}