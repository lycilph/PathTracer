using Core.Algebra;

namespace ScriptApi.Validation;

/// <summary>
/// Validates a scene configuration before building it.
/// Produces errors for issues that prevent correct rendering and
/// warnings for issues that may produce unexpected results.
/// </summary>
internal static class SceneValidator
{
    /// <summary>
    /// Validates the complete scene configuration.
    /// </summary>
    internal static ValidationResult Validate(SceneBuilderState state)
    {
        var result = new ValidationResult();

        ValidateCamera(state, result);
        ValidateRenderSettings(state, result);
        ValidateGeometry(state, result);
        ValidateLights(state, result);
        ValidatePrimitiveNames(state, result);

        return result;
    }

    private static void ValidateCamera(SceneBuilderState state,
                                       ValidationResult result)
    {
        if (!state.HasCamera)
        {
            result.AddError("No camera defined. Call WithCamera() before Build().");
            return;
        }

        if (state.FovDegrees <= 0 || state.FovDegrees >= 180)
            result.AddError(
                $"Field of view must be in (0, 180) degrees, got {state.FovDegrees}.");

        if (state.FocusDistance <= 0)
            result.AddError(
                $"Focus distance must be positive, got {state.FocusDistance}.");

        if (state.Aperture < 0)
            result.AddError(
                $"Aperture cannot be negative, got {state.Aperture}.");

        if ((state.LookAt - state.CameraPosition).IsNearZero())
            result.AddError(
                "Camera position and look-at point are the same — " +
                "cannot determine view direction.");
    }

    private static void ValidateRenderSettings(SceneBuilderState state,
                                               ValidationResult result)
    {
        if (!state.HasRenderSettings)
        {
            result.AddError(
                "No render settings defined. Call WithRenderSettings() before Build().");
            return;
        }

        if (state.ImageWidth <= 0)
            result.AddError(
                $"Image width must be positive, got {state.ImageWidth}.");

        if (state.ImageHeight <= 0)
            result.AddError(
                $"Image height must be positive, got {state.ImageHeight}.");

        if (state.SamplesPerPixel <= 0)
            result.AddError(
                $"Samples per pixel must be positive, got {state.SamplesPerPixel}.");

        if (state.SamplesPerPixel < 4)
            result.AddWarning(
                $"Samples per pixel is very low ({state.SamplesPerPixel}). " +
                "The image will be very noisy.");
    }

    private static void ValidateGeometry(SceneBuilderState state,
                                         ValidationResult result)
    {
        if (state.Primitives.Count == 0)
        {
            result.AddWarning("No geometry in scene. The image will be black.");
            return;
        }

        foreach (var primitive in state.Primitives)
            ValidatePrimitive(primitive, result);
    }

    private static void ValidatePrimitive(PrimitiveEntry primitive,
                                          ValidationResult result)
    {
        switch (primitive)
        {
            case SpherePrimitive s:
                if (s.Radius <= 0)
                    result.AddError(
                        $"Sphere radius must be positive, got {s.Radius}.",
                        s.Name);
                if (s.Radius < 1e-4)
                    result.AddWarning(
                        $"Sphere radius is very small ({s.Radius}). " +
                        "This may cause precision issues.",
                        s.Name);
                ValidateMaterial(s.Material, s.Name, result);
                break;

            case QuadPrimitive q:
                if (q.Edge1.IsNearZero())
                    result.AddError("Quad edge1 has zero length.", q.Name);
                if (q.Edge2.IsNearZero())
                    result.AddError("Quad edge2 has zero length.", q.Name);
                if (!q.Edge1.IsNearZero() && !q.Edge2.IsNearZero())
                {
                    var normal = Vector3.Cross(q.Edge1, q.Edge2);
                    if (normal.IsNearZero())
                        result.AddError(
                            "Quad edges are parallel — degenerate geometry.",
                            q.Name);
                }
                ValidateMaterial(q.Material, q.Name, result);
                break;

            case AreaLightPrimitive a:
                if (a.Edge1.IsNearZero())
                    result.AddError("Area light edge1 has zero length.", a.Name);
                if (a.Edge2.IsNearZero())
                    result.AddError("Area light edge2 has zero length.", a.Name);
                if (a.Emission.X < 0 || a.Emission.Y < 0 || a.Emission.Z < 0)
                    result.AddError(
                        "Area light emission cannot be negative.", a.Name);
                if (a.Emission.X > 100 || a.Emission.Y > 100 || a.Emission.Z > 100)
                    result.AddWarning(
                        $"Area light emission is very high ({a.Emission}). " +
                        "This may cause fireflies.",
                        a.Name);
                break;

            case MeshPrimitive m:
                if (!File.Exists(m.Path))
                    result.AddError(
                        $"Mesh file not found: {m.Path}", m.Name);
                ValidateMaterial(m.Material, m.Name, result);
                break;
        }
    }

    private static void ValidateMaterial(Core.IMaterial material,
                                         string? name,
                                         ValidationResult result)
    {
        switch (material)
        {
            case Engine.Materials.Lambertian l:
                if (l.Albedo.X < 0 || l.Albedo.Y < 0 || l.Albedo.Z < 0)
                    result.AddError(
                        "Lambertian albedo cannot be negative.", name);
                if (l.Albedo.X > 1 || l.Albedo.Y > 1 || l.Albedo.Z > 1)
                    result.AddWarning(
                        $"Lambertian albedo ({l.Albedo}) exceeds 1 — " +
                        "violates energy conservation.",
                        name);
                break;

            case Engine.Materials.GgxMetal g:
                if (g.Roughness < 0 || g.Roughness > 1)
                    result.AddWarning(
                        $"GGX roughness ({g.Roughness}) is outside [0,1].",
                        name);
                if (g.F0.X < 0 || g.F0.Y < 0 || g.F0.Z < 0)
                    result.AddError(
                        "GGX F0 reflectance cannot be negative.", name);
                if (g.F0.X > 1 || g.F0.Y > 1 || g.F0.Z > 1)
                    result.AddWarning(
                        $"GGX F0 reflectance ({g.F0}) exceeds 1 — " +
                        "not physically plausible.",
                        name);
                break;

            case Engine.Materials.Dielectric d:
                if (d.Ior < 1.0)
                    result.AddWarning(
                        $"Dielectric IOR ({d.Ior}) is below 1.0 — " +
                        "not physically plausible for most materials.",
                        name);
                break;

            case Engine.Materials.Emissive e:
                if (e.Emission.X < 0 || e.Emission.Y < 0 || e.Emission.Z < 0)
                    result.AddError(
                        "Emissive emission cannot be negative.", name);
                break;
        }
    }

    private static void ValidateLights(SceneBuilderState state,
                                       ValidationResult result)
    {
        if (state.Primitives.All(p => p is not AreaLightPrimitive))
            result.AddWarning(
                "No lights in scene. The image will be black unless " +
                "emissive materials or a non-zero background are used.");
    }

    private static void ValidatePrimitiveNames(SceneBuilderState state,
                                               ValidationResult result)
    {
        var names = state.Primitives
            .Select(p => p.Name)
            .Where(n => n is not null)
            .ToList();

        var duplicates = names
            .GroupBy(n => n)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        foreach (var duplicate in duplicates)
            result.AddWarning(
                $"Duplicate primitive name '{duplicate}'. " +
                "Names should be unique for unambiguous debugging.");
    }
}
