namespace Core;

/// <summary>
/// A camera supporting pinhole rendering, thin-lens depth of field,
/// and motion blur via shutter time (§3.2.1, §3.2.2).
/// </summary>
public sealed class Camera
{
    private readonly Vector3 _origin;
    private readonly Vector3 _horizontal;
    private readonly Vector3 _vertical;
    private readonly Vector3 _lowerLeft;
    private readonly Vector3 _right;
    private readonly Vector3 _up;
    private readonly double _lensRadius;
    private readonly double _shutterOpen;
    private readonly double _shutterClose;

    public int ImageWidth { get; }
    public int ImageHeight { get; }

    /// <param name="position">Camera position in world space.</param>
    /// <param name="lookAt">The point the camera is aimed at.</param>
    /// <param name="up">World-space up vector.</param>
    /// <param name="vFovDegrees">Vertical field of view in degrees.</param>
    /// <param name="imageWidth">Output image width in pixels.</param>
    /// <param name="imageHeight">Output image height in pixels.</param>
    /// <param name="aperture">
    /// Lens diameter in world units. 0 = pinhole (no DoF). Default 0.
    /// </param>
    /// <param name="focusDistance">
    /// Distance from the camera to the plane of perfect focus.
    /// Ignored when aperture = 0. Default 1.
    /// </param>
    /// <param name="shutterOpen">Shutter open time for motion blur. Default 0.</param>
    /// <param name="shutterClose">Shutter close time for motion blur. Default 0.</param>
    public Camera(
        Vector3 position,
        Vector3 lookAt,
        Vector3 up,
        double vFovDegrees,
        int imageWidth,
        int imageHeight,
        double aperture = 0.0,
        double focusDistance = 1.0,
        double shutterOpen = 0.0,
        double shutterClose = 0.0)
    {
        ImageWidth = imageWidth;
        ImageHeight = imageHeight;
        _lensRadius = aperture / 2.0;
        _shutterOpen = shutterOpen;
        _shutterClose = shutterClose;

        var theta = vFovDegrees * Math.PI / 180.0;
        var halfHeight = Math.Tan(theta / 2.0);
        var halfWidth = halfHeight * imageWidth / imageHeight;

        var forward = (lookAt - position).Normalize();
        _right = Vector3.Cross(forward, up).Normalize();
        _up = Vector3.Cross(_right, forward);

        _origin = position;
        _horizontal = 2.0 * halfWidth * focusDistance * _right;
        _vertical = 2.0 * halfHeight * focusDistance * _up;
        _lowerLeft = position
                    + focusDistance * forward
                    - 0.5 * _horizontal
                    - 0.5 * _vertical;
    }

    /// <summary>
    /// Generates a ray through pixel (i, j) with optional DoF jitter
    /// and motion blur time sampling (§3.2.1, §3.2.2).
    /// </summary>
    /// <param name="i">Pixel column, zero-based from the left.</param>
    /// <param name="j">Pixel row, zero-based from the top.</param>
    /// <param name="jitterU">Sub-pixel jitter in [0,1) along horizontal axis.</param>
    /// <param name="jitterV">Sub-pixel jitter in [0,1) along vertical axis.</param>
    /// <param name="sampler">
    /// Per-thread sampler used for lens disk sampling and shutter time.
    /// Pass null for a pinhole camera with no motion blur.
    /// </param>
    public Ray GenerateRay(int i, int j,
                           double jitterU = 0.5, double jitterV = 0.5,
                           Sampler? sampler = null)
    {
        var u = (i + jitterU) / ImageWidth;
        var v = 1.0 - (j + jitterV) / ImageHeight;

        var target = _lowerLeft + u * _horizontal + v * _vertical;

        // §3.2.2 Thin-lens DoF: jitter origin within lens disk
        var origin = _origin;
        if (_lensRadius > 0 && sampler is not null)
        {
            var lensPoint = _lensRadius * SampleDisk(sampler);
            origin = _origin + lensPoint.X * _right + lensPoint.Y * _up;
        }

        var direction = (target - origin).Normalize();

        // §3.2.2 Motion blur: sample a random time within the shutter interval
        var time = 0.0;
        if (_shutterClose > _shutterOpen && sampler is not null)
            time = _shutterOpen +
                   sampler.Next() * (_shutterClose - _shutterOpen);

        return new Ray(origin, direction, Time: time);
    }

    /// <summary>
    /// Samples a point uniformly within the unit disk using rejection sampling.
    /// Used for thin-lens depth of field (§3.2.2).
    /// </summary>
    private static Vector3 SampleDisk(Sampler sampler)
    {
        while (true)
        {
            var p = new Vector3(
                sampler.Next(-1, 1),
                sampler.Next(-1, 1),
                0);
            if (p.LengthSquared < 1.0)
                return p;
        }
    }
}