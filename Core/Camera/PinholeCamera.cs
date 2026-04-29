using Core.Math;
using Core.Sampling;

namespace Core.Camera;

/// <summary>
/// Pinhole camera with optional shutter interval for motion blur.
/// </summary>
public sealed class PinholeCamera : ICamera
{
    private readonly Vec3 _origin;
    private readonly Vec3 _horizontal;
    private readonly Vec3 _vertical;
    private readonly Vec3 _lowerLeftCorner;

    private readonly float _shutterOpen;
    private readonly float _shutterClose;

    public PinholeCamera(
        float vfovDegrees,
        float aspectRatio,
        in Vec3 lookFrom,
        in Vec3 lookAt,
        in Vec3 vUp,
        float shutterOpen = 0f,
        float shutterClose = 0f)
    {
        _origin = lookFrom;

        _shutterOpen = shutterOpen;
        _shutterClose = shutterClose;

        float theta = vfovDegrees * (MathUtil.Pi / 180f);
        float h = float.Tan(theta / 2f);
        float viewportHeight = 2f * h;
        float viewportWidth = aspectRatio * viewportHeight;

        Vec3 w = (lookFrom - lookAt).Normalized();
        Vec3 u = Vec3.Cross(vUp, w).Normalized();
        Vec3 v = Vec3.Cross(w, u);

        _horizontal = u * viewportWidth;
        _vertical = v * viewportHeight;
        _lowerLeftCorner = _origin - _horizontal / 2f - _vertical / 2f - w;
    }

    public Ray GetRay(float u, float v, Sampler sampler)
    {
        var dir = _lowerLeftCorner + _horizontal * u + _vertical * v - _origin;
        float time = SampleTime(sampler);
        return new Ray(_origin, dir, time);
    }

    private float SampleTime(Sampler sampler)
    {
        if (_shutterClose <= _shutterOpen)
            return _shutterOpen;

        float t = sampler.Next1D();
        return MathUtil.Lerp(_shutterOpen, _shutterClose, t);
    }
}