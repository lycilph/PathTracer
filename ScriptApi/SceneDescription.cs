using Core;
using Core.Algebra;
using Core.Geometry;
using Engine.Rendering;
using ScriptApi.Validation;

namespace ScriptApi;

/// <summary>
/// The fully built result of a scene script. Contains everything needed
/// to render the scene without any additional configuration.
/// </summary>
public sealed class SceneDescription
{
    /// <summary>The camera to render from.</summary>
    public Camera Camera { get; }

    /// <summary>All render settings for this scene.</summary>
    public RenderSettings Settings { get; }

    /// <summary>
    /// The fully built scene geometry, accelerated by a BVH where
    /// the primitive count justifies it.
    /// </summary>
    public IHittable Scene { get; }

    /// <summary>All samplable lights in the scene.</summary>
    public IReadOnlyList<ILight> Lights { get; }

    /// <summary>
    /// Validation result from scene construction. Always check
    /// <see cref="ValidationResult.IsValid"/> and inspect
    /// <see cref="ValidationResult.Warnings"/> before rendering.
    /// </summary>
    public ValidationResult Validation { get; }

    /// <summary>
    /// Total number of primitives in the scene including area lights.
    /// </summary>
    public int PrimitiveCount { get; }

    internal SceneDescription(
        Camera camera,
        RenderSettings settings,
        IHittable scene,
        IReadOnlyList<ILight> lights,
        int primitiveCount,
        ValidationResult validation)
    {
        Camera = camera;
        Settings = settings;
        Scene = scene;
        Lights = lights;
        PrimitiveCount = primitiveCount;
        Validation = validation;
    }
}