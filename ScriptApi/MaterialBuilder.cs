using Core;
using Core.Algebra;
using Engine.Materials;

namespace ScriptApi;

/// <summary>
/// Factory methods for creating materials to use in scene scripts.
/// All methods return an <see cref="IMaterial"/> ready to pass to
/// geometry creation methods on <see cref="SceneBuilder"/>.
/// </summary>
public static class MaterialBuilder
{
    /// <summary>
    /// Creates an ideal diffuse (Lambertian) material (§3.8.1).
    /// </summary>
    /// <param name="albedo">
    /// Fraction of light reflected per RGB channel. Each component
    /// should be in [0,1] for physical plausibility.
    /// </param>
    public static IMaterial Lambertian(Vector3 albedo) => new Lambertian(albedo);

    /// <summary>
    /// Creates a perfect mirror material (§3.8.2).
    /// </summary>
    /// <param name="reflectance">
    /// Fraction of light reflected per RGB channel. Each component
    /// should be in [0,1]. Default is full white reflectance.
    /// </param>
    public static IMaterial Mirror(Vector3? reflectance = null) => new Mirror(reflectance ?? Vector3.One);

    /// <summary>
    /// Creates a GGX microfacet metallic material (§3.8.3).
    /// </summary>
    /// <param name="f0">
    /// Reflectance at normal incidence per RGB channel.
    /// Silver ≈ (0.95, 0.93, 0.88). Each component in [0,1].
    /// </param>
    /// <param name="roughness">
    /// Surface roughness α in [0,1]. 0 = perfect mirror,
    /// 1 = fully diffuse-looking.
    /// </param>
    public static IMaterial GgxMetal(Vector3 f0, double roughness) => new GgxMetal(f0, roughness);

    /// <summary>
    /// Creates a dielectric (glass) material (§3.8.4).
    /// </summary>
    /// <param name="ior">
    /// Index of refraction. Air = 1.0, borosilicate glass ≈ 1.5,
    /// diamond ≈ 2.4.
    /// </param>
    public static IMaterial Dielectric(double ior) => new Dielectric(ior);

    /// <summary>
    /// Creates a light-emitting material (§3.9.1).
    /// </summary>
    /// <param name="emission">
    /// Emitted radiance per RGB channel. HDR values above 1 are valid
    /// and common for bright light sources.
    /// </param>
    public static IMaterial Emissive(Vector3 emission) => new Emissive(emission);
}