using Core.Algebra;

namespace ScriptApi;

/// <summary>
/// Static entry point for building a scene description using the fluent API.
/// </summary>
/// <example>
/// <code>
/// var result = Scene
///     .WithCamera(
///         position: new Vector3(0, 0, 3.5),
///         lookAt: Vector3.Zero,
///         fovDegrees: 40)
///     .WithRenderSettings(
///         imageWidth: 512,
///         imageHeight: 512,
///         samplesPerPixel: 256)
///     .AddSphere(
///         centre: new Vector3(0, 0, 0),
///         radius: 0.5,
///         material: MaterialBuilder.Lambertian(new Vector3(0.8, 0.5, 0.3)),
///         name: "TestSphere")
///     .AddAreaLight(
///         corner: new Vector3(-0.25, 0.999, -0.25),
///         edge1: new Vector3(0.5, 0, 0),
///         edge2: new Vector3(0, 0, 0.5),
///         emission: new Vector3(15, 15, 15),
///         name: "CeilingLight")
///     .Build();
///
/// if (result.Validation.IsValid)
///     // render...
/// </code>
/// </example>
public static class Scene
{
    /// <summary>
    /// Starts building a scene with the given camera configuration.
    /// Chain further calls to configure the scene, then call
    /// <see cref="SceneBuilder.Build"/> to produce a
    /// <see cref="SceneDescription"/>.
    /// </summary>
    /// <param name="position">Camera position in world space.</param>
    /// <param name="lookAt">The point the camera is aimed at.</param>
    /// <param name="fovDegrees">Vertical field of view in degrees.</param>
    /// <param name="up">World-space up vector. Default is Y-up.</param>
    /// <param name="aperture">
    /// Lens diameter in world units. 0 = pinhole (no DoF).
    /// </param>
    /// <param name="focusDistance">
    /// Distance to the plane of perfect focus. Ignored when aperture = 0.
    /// </param>
    /// <param name="shutterOpen">Shutter open time for motion blur.</param>
    /// <param name="shutterClose">Shutter close time for motion blur.</param>
    public static SceneBuilder WithCamera(
        Vector3 position,
        Vector3 lookAt,
        double fovDegrees,
        Vector3? up = null,
        double aperture = 0.0,
        double focusDistance = 1.0,
        double shutterOpen = 0.0,
        double shutterClose = 0.0)
        => new SceneBuilder().WithCamera(
            position,
            lookAt,
            fovDegrees,
            up,
            aperture,
            focusDistance,
            shutterOpen,
            shutterClose);
}