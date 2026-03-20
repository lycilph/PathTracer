using Core;
using Core.Acceleration;
using Core.Algebra;
using Core.Geometry;
using Engine.Lighting;
using Engine.Rendering;
using ScriptApi.Validation;

namespace ScriptApi;

// ── Primitive entry types ─────────────────────────────────────────────────────

/// <summary>Base class for all primitive entries stored during scene building.</summary>
internal abstract class PrimitiveEntry
{
    public string? Name { get; init; }
}

internal sealed class SpherePrimitive : PrimitiveEntry
{
    public Vector3 Centre { get; init; }
    public double Radius { get; init; }
    public IMaterial Material { get; init; } = null!;
}

internal sealed class QuadPrimitive : PrimitiveEntry
{
    public Vector3 Corner { get; init; }
    public Vector3 Edge1 { get; init; }
    public Vector3 Edge2 { get; init; }
    public IMaterial Material { get; init; } = null!;
}

internal sealed class AreaLightPrimitive : PrimitiveEntry
{
    public Vector3 Corner { get; init; }
    public Vector3 Edge1 { get; init; }
    public Vector3 Edge2 { get; init; }
    public Vector3 Emission { get; init; }
}

internal sealed class MeshPrimitive : PrimitiveEntry
{
    public string Path { get; init; } = null!;
    public IMaterial Material { get; init; } = null!;
    public bool SmoothNormals { get; init; }
    public Matrix4x4d Transform { get; init; } = Matrix4x4d.Identity;
}

// ── SceneBuilderState ─────────────────────────────────────────────────────────

/// <summary>
/// Holds the raw accumulated state of the builder before validation and
/// construction. Passed to <see cref="SceneValidator"/> during Build().
/// </summary>
internal sealed class SceneBuilderState
{
    // Camera
    public bool HasCamera { get; set; }
    public Vector3 CameraPosition { get; set; }
    public Vector3 LookAt { get; set; }
    public Vector3 Up { get; set; } = Vector3.UnitY;
    public double FovDegrees { get; set; }
    public double Aperture { get; set; }
    public double FocusDistance { get; set; } = 1.0;
    public double ShutterOpen { get; set; }
    public double ShutterClose { get; set; }

    // Render settings
    public bool HasRenderSettings { get; set; }
    public int ImageWidth { get; set; }
    public int ImageHeight { get; set; }
    public int SamplesPerPixel { get; set; }
    public Vector3 BackgroundRadiance { get; set; }

    // Primitives
    public List<PrimitiveEntry> Primitives { get; } = [];
}

// ── SceneBuilder ──────────────────────────────────────────────────────────────

/// <summary>
/// Fluent builder for constructing a <see cref="SceneDescription"/>.
/// Start with <see cref="Scene.WithCamera"/> and finish with
/// <see cref="Build"/>.
/// </summary>
public sealed class SceneBuilder
{
    private readonly SceneBuilderState _state = new();

    public SceneBuilder() { }

    // ── Camera ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Configures the camera for this scene.
    /// </summary>
    /// <param name="position">Camera position in world space.</param>
    /// <param name="lookAt">The point the camera is aimed at.</param>
    /// <param name="up">World-space up vector. Default is Y-up.</param>
    /// <param name="fovDegrees">Vertical field of view in degrees.</param>
    /// <param name="aperture">
    /// Lens diameter in world units. 0 = pinhole (no DoF).
    /// </param>
    /// <param name="focusDistance">
    /// Distance to the plane of perfect focus. Ignored when aperture = 0.
    /// </param>
    /// <param name="shutterOpen">Shutter open time for motion blur.</param>
    /// <param name="shutterClose">Shutter close time for motion blur.</param>
    public SceneBuilder WithCamera(
        Vector3 position,
        Vector3 lookAt,
        double fovDegrees,
        Vector3? up = null,
        double aperture = 0.0,
        double focusDistance = 1.0,
        double shutterOpen = 0.0,
        double shutterClose = 0.0)
    {
        _state.HasCamera = true;
        _state.CameraPosition = position;
        _state.LookAt = lookAt;
        _state.Up = up ?? Vector3.UnitY;
        _state.FovDegrees = fovDegrees;
        _state.Aperture = aperture;
        _state.FocusDistance = focusDistance;
        _state.ShutterOpen = shutterOpen;
        _state.ShutterClose = shutterClose;
        return this;
    }

    // ── Render settings ───────────────────────────────────────────────────────

    /// <summary>
    /// Configures the render settings for this scene.
    /// </summary>
    /// <param name="imageWidth">Output image width in pixels.</param>
    /// <param name="imageHeight">Output image height in pixels.</param>
    /// <param name="samplesPerPixel">Number of samples per pixel.</param>
    /// <param name="backgroundRadiance">
    /// Radiance returned when a ray escapes the scene.
    /// Default is black (Vector3.Zero).
    /// </param>
    public SceneBuilder WithRenderSettings(
        int imageWidth,
        int imageHeight,
        int samplesPerPixel,
        Vector3? backgroundRadiance = null)
    {
        _state.HasRenderSettings = true;
        _state.ImageWidth = imageWidth;
        _state.ImageHeight = imageHeight;
        _state.SamplesPerPixel = samplesPerPixel;
        _state.BackgroundRadiance = backgroundRadiance ?? Vector3.Zero;
        return this;
    }

    // ── Geometry ──────────────────────────────────────────────────────────────

    /// <summary>Adds a sphere to the scene.</summary>
    /// <param name="centre">Centre of the sphere in world space.</param>
    /// <param name="radius">Radius of the sphere in world units.</param>
    /// <param name="material">Surface material.</param>
    /// <param name="name">Optional name for debugging.</param>
    public SceneBuilder AddSphere(
        Vector3 centre,
        double radius,
        IMaterial material,
        string? name = null)
    {
        _state.Primitives.Add(new SpherePrimitive
        {
            Name = name,
            Centre = centre,
            Radius = radius,
            Material = material
        });
        return this;
    }

    /// <summary>Adds a quad (parallelogram) to the scene.</summary>
    /// <param name="corner">One corner of the quad in world space.</param>
    /// <param name="edge1">First edge vector in world units.</param>
    /// <param name="edge2">Second edge vector in world units.</param>
    /// <param name="material">Surface material.</param>
    /// <param name="name">Optional name for debugging.</param>
    public SceneBuilder AddQuad(
        Vector3 corner,
        Vector3 edge1,
        Vector3 edge2,
        IMaterial material,
        string? name = null)
    {
        _state.Primitives.Add(new QuadPrimitive
        {
            Name = name,
            Corner = corner,
            Edge1 = edge1,
            Edge2 = edge2,
            Material = material
        });
        return this;
    }

    /// <summary>Adds a rectangular area light to the scene.</summary>
    /// <param name="corner">One corner of the light in world space.</param>
    /// <param name="edge1">First edge vector in world units.</param>
    /// <param name="edge2">Second edge vector in world units.</param>
    /// <param name="emission">Emitted radiance. HDR values above 1 are valid.</param>
    /// <param name="name">Optional name for debugging.</param>
    public SceneBuilder AddAreaLight(
        Vector3 corner,
        Vector3 edge1,
        Vector3 edge2,
        Vector3 emission,
        string? name = null)
    {
        _state.Primitives.Add(new AreaLightPrimitive
        {
            Name = name,
            Corner = corner,
            Edge1 = edge1,
            Edge2 = edge2,
            Emission = emission
        });
        return this;
    }

    /// <summary>Adds an OBJ mesh to the scene.</summary>
    /// <param name="path">Path to the .obj file.</param>
    /// <param name="material">Material applied to all triangles.</param>
    /// <param name="transform">
    /// Optional world transform. Default is identity (no transform).
    /// Use <see cref="Matrix4x4d"/> factory methods to build transforms.
    /// </param>
    /// <param name="smoothNormals">
    /// If true and the OBJ contains vertex normals, smooth shading is used.
    /// </param>
    /// <param name="name">Optional name for debugging.</param>
    public SceneBuilder AddMesh(
        string path,
        IMaterial material,
        Matrix4x4d? transform = null,
        bool smoothNormals = false,
        string? name = null)
    {
        _state.Primitives.Add(new MeshPrimitive
        {
            Name = name,
            Path = path,
            Material = material,
            Transform = transform ?? Matrix4x4d.Identity,
            SmoothNormals = smoothNormals
        });
        return this;
    }

    // ── Build ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Validates and builds the scene description.
    /// Always check <see cref="SceneDescription.Validation"/> on the
    /// returned result before rendering.
    /// </summary>
    public SceneDescription Build()
    {
        var validation = SceneValidator.Validate(_state);

        // Build camera — use defaults if missing so we can still return
        // a result even when validation has errors
        var camera = _state.HasCamera
            ? new Camera(
                _state.CameraPosition,
                _state.LookAt,
                _state.Up,
                _state.FovDegrees,
                _state.HasRenderSettings ? _state.ImageWidth : 512,
                _state.HasRenderSettings ? _state.ImageHeight : 512,
                _state.Aperture,
                _state.FocusDistance,
                _state.ShutterOpen,
                _state.ShutterClose)
            : new Camera(
                new Vector3(0, 0, 3.5), Vector3.Zero,
                Vector3.UnitY, 40, 512, 512);

        var settings = _state.HasRenderSettings
            ? new RenderSettings(
                _state.ImageWidth,
                _state.ImageHeight,
                _state.SamplesPerPixel,
                _state.BackgroundRadiance)
            : new RenderSettings(512, 512, 64, Vector3.Zero);

        // Build geometry
        var sceneList = new SceneList();
        var lights = new List<ILight>();

        foreach (var primitive in _state.Primitives)
        {
            switch (primitive)
            {
                case SpherePrimitive s:
                    sceneList.Add(new Sphere(s.Centre, s.Radius, s.Material));
                    break;

                case QuadPrimitive q:
                    sceneList.Add(new Quad(q.Corner, q.Edge1, q.Edge2, q.Material));
                    break;

                case AreaLightPrimitive a:
                    var light = new AreaLight(a.Corner, a.Edge1, a.Edge2, a.Emission);
                    sceneList.Add(light);
                    lights.Add(light);
                    break;

                case MeshPrimitive m:
                    var mesh = Mesh.Load(m.Path, m.Material, m.SmoothNormals);
                    var isIdentity = m.Transform.Equals(Matrix4x4d.Identity);
                    IHittable hittable = isIdentity
                        ? mesh
                        : new Transform(mesh, m.Transform);
                    sceneList.Add(hittable);
                    break;
            }
        }

        var scene = sceneList.Build();
        var primitiveCount = _state.Primitives.Count;

        return new SceneDescription(camera, settings, scene, lights, primitiveCount, validation);
    }
}