using Core.Math;
using Core.Sampling;

namespace Core.Camera;

/// <summary>
/// Simple pinhole camera producing rays through an image plane.
/// Coordinate conventions are documented in docs/conventions.md.
/// </summary>
public sealed class PinholeCamera : ICamera
{
    private readonly Vec3 _origin;
    private readonly Vec3 _horizontal;
    private readonly Vec3 _vertical;
    private readonly Vec3 _lowerLeftCorner;
    private readonly float _time;

    public PinholeCamera(
        float vfovDegrees,
        float aspectRatio,
        in Vec3 lookFrom,
        in Vec3 lookAt,
        in Vec3 vUp,
        float time = 0f)
    {
        _origin = lookFrom;
        _time = time;

        float theta = vfovDegrees * (MathUtil.Pi / 180f);
        float h = float.Tan(theta / 2f);
        float viewportHeight = 2f * h;
        float viewportWidth = aspectRatio * viewportHeight;

        // Camera basis
        Vec3 w = (lookFrom - lookAt).Normalized();      // backward
        Vec3 u = Vec3.Cross(vUp, w).Normalized();       // right
        Vec3 v = Vec3.Cross(w, u);                      // up

        _horizontal = u * viewportWidth;
        _vertical = v * viewportHeight;
        _lowerLeftCorner = _origin - _horizontal / 2f - _vertical / 2f - w; // image plane at distance 1
    }

    public Ray GetRay(float u, float v, Sampler sampler)
    {
        var dir = _lowerLeftCorner + _horizontal * u + _vertical * v - _origin;
        return new Ray(_origin, dir, _time);
    }
}