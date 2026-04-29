using Core.Math;
using Core.Sampling;

namespace Core.Camera;

/// <summary>
/// Thin-lens camera for depth of field.
/// - apertureRadius: lens radius in scene units
/// - focusDistance: distance to the focus plane along camera forward direction
/// </summary>
public sealed class ThinLensCamera : ICamera
{
    private readonly Vec3 _origin;
    private readonly Vec3 _u; // right
    private readonly Vec3 _v; // up
    private readonly Vec3 _w; // backward
    private readonly Vec3 _horizontal;
    private readonly Vec3 _vertical;
    private readonly Vec3 _lowerLeftCorner;
    private readonly float _time;

    public float ApertureRadius { get; }
    public float FocusDistance { get; }

    public ThinLensCamera(
        float vfovDegrees,
        float aspectRatio,
        in Vec3 lookFrom,
        in Vec3 lookAt,
        in Vec3 vUp,
        float focusDistance,
        float apertureRadius,
        float time = 0f)
    {
        _origin = lookFrom;
        _time = time;

        FocusDistance = focusDistance;
        ApertureRadius = apertureRadius;

        float theta = vfovDegrees * (MathUtil.Pi / 180f);
        float h = float.Tan(theta / 2f);
        float viewportHeight = 2f * h;
        float viewportWidth = aspectRatio * viewportHeight;

        _w = (lookFrom - lookAt).Normalized();     // backward
        _u = Vec3.Cross(vUp, _w).Normalized();     // right
        _v = Vec3.Cross(_w, _u);                   // up

        // Image plane at distance 1 along -w
        _horizontal = _u * viewportWidth;
        _vertical = _v * viewportHeight;
        _lowerLeftCorner = _origin - _horizontal / 2f - _vertical / 2f - _w;
    }

    public Ray GetRay(float u, float v, Sampler sampler)
    {
        // Pinhole ray direction to pixel on image plane
        Vec3 dir = _lowerLeftCorner + _horizontal * u + _vertical * v - _origin;

        // Focus plane is perpendicular to forward (-w) at distance FocusDistance from origin
        Vec3 forward = (-_w).Normalized();
        float denom = Vec3.Dot(dir, forward);
        if (denom <= 1e-8f)
        {
            // Degenerate; fall back to pinhole
            return new Ray(_origin, dir, _time);
        }

        float tFocus = FocusDistance / denom;
        Vec3 focusPoint = _origin + dir * tFocus;

        // Sample point on lens
        Vec3 disk = SamplingUtil.ConcentricSampleDisk(sampler.Next1D(), sampler.Next1D());
        Vec3 lensOffset = (_u * disk.X + _v * disk.Y) * ApertureRadius;
        Vec3 lensPoint = _origin + lensOffset;

        Vec3 finalDir = (focusPoint - lensPoint).Normalized();
        return new Ray(lensPoint, finalDir, _time);
    }
}