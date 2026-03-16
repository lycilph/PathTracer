namespace Core;

/// <summary>
/// A pinhole camera that generates primary rays for each pixel (§3.2.1).
/// </summary>
public sealed class Camera
{
    private readonly Vector3 _origin;
    private readonly Vector3 _horizontal;   // full viewport width vector
    private readonly Vector3 _vertical;     // full viewport height vector
    private readonly Vector3 _lowerLeft;    // world-space corner of the viewport

    /// <summary>Image width in pixels.</summary>
    public int ImageWidth { get; }

    /// <summary>Image height in pixels.</summary>
    public int ImageHeight { get; }

    /// <param name="position">Camera position in world space.</param>
    /// <param name="lookAt">The point the camera is aimed at.</param>
    /// <param name="up">World-space up vector (usually 0,1,0).</param>
    /// <param name="vFovDegrees">Vertical field of view in degrees.</param>
    /// <param name="imageWidth">Output image width in pixels.</param>
    /// <param name="imageHeight">Output image height in pixels.</param>
    public Camera(
        Vector3 position,
        Vector3 lookAt,
        Vector3 up,
        double vFovDegrees,
        int imageWidth,
        int imageHeight)
    {
        ImageWidth = imageWidth;
        ImageHeight = imageHeight;

        // §3.2.1 — build the viewport from the field of view
        var theta = vFovDegrees * Math.PI / 180.0;
        var halfHeight = Math.Tan(theta / 2.0);
        var halfWidth = halfHeight * imageWidth / imageHeight;

        // Orthonormal camera basis
        var forward = (lookAt - position).Normalize();   //  -Z in camera space
        var right = Vector3.Cross(forward, up).Normalize();
        var cameraUp = Vector3.Cross(right, forward);     // recomputed for orthogonality

        _origin = position;
        _horizontal = 2.0 * halfWidth * right;
        _vertical = 2.0 * halfHeight * cameraUp;

        // Lower-left corner of the viewport in world space
        _lowerLeft = position + forward - 0.5 * _horizontal - 0.5 * _vertical;
    }

    /// <summary>
    /// Generates a ray from the camera through pixel (i, j) with sub-pixel jitter
    /// for anti-aliasing.
    /// </summary>
    /// <param name="i">Pixel column, zero-based from the left.</param>
    /// <param name="j">Pixel row, zero-based from the top.</param>
    /// <param name="jitterU">Random offset in [0,1) along the horizontal axis.</param>
    /// <param name="jitterV">Random offset in [0,1) along the vertical axis.</param>
    /// <returns>A ray from the camera origin through the jittered pixel position.</returns>
    public Ray GenerateRay(int i, int j, double jitterU = 0.5, double jitterV = 0.5)
    {
        // Normalised pixel coordinates with jitter, V flipped so row 0 is top
        var u = (i + jitterU) / ImageWidth;
        var v = 1.0 - (j + jitterV) / ImageHeight;

        var target = _lowerLeft + u * _horizontal + v * _vertical;
        var direction = (target - _origin).Normalize();

        return new Ray(_origin, direction);
    }
}