using Core;
using Core.Acceleration;
using Core.Algebra;
using Core.Geometry;
using Engine.Integrators;
using Engine.Lighting;
using Engine.Materials;
using Engine.Rendering;
using Engine.Scene;

namespace Render;

internal class Program
{
    private const int Width = 512;
    private const int Height = 512;
    private const int SamplesPerPixel = 256;

    static void Main(string[] args)
    {
        // Pick which scene to render by uncommenting:
        //RenderCornellClassic();
        // RenderCornellBunny();
        RenderCornellDofAndMotionBlur();
    }

    /// <summary>
    /// The canonical Cornell Box from Milestone 1 — glass sphere, silver
    /// metallic sphere, area light. Validates global illumination, soft
    /// shadows, refraction and reflection.
    /// </summary>
    private static void RenderCornellClassic()
    {
        var scene = new SceneList();
        var lights = new List<ILight>();

        var white = new Lambertian(new Vector3(0.73, 0.73, 0.73));
        var red = new Lambertian(new Vector3(0.65, 0.05, 0.05));
        var green = new Lambertian(new Vector3(0.12, 0.45, 0.15));
        var glass = new Dielectric(Ior: 1.5);
        var silver = new GgxMetal(
            F0: new Vector3(0.95, 0.93, 0.88),
            Roughness: 0.05);

        // Cornell Box walls
        scene.Add(new Quad(new Vector3(-1, -1, -1), new Vector3(2, 0, 0),
                           new Vector3(0, 0, 2), white));
        scene.Add(new Quad(new Vector3(-1, 1, -1), new Vector3(2, 0, 0),
                           new Vector3(0, 0, 2), white));
        scene.Add(new Quad(new Vector3(-1, -1, -1), new Vector3(2, 0, 0),
                           new Vector3(0, 2, 0), white));
        scene.Add(new Quad(new Vector3(-1, -1, -1), new Vector3(0, 2, 0),
                           new Vector3(0, 0, 2), red));
        scene.Add(new Quad(new Vector3(1, -1, -1), new Vector3(0, 2, 0),
                           new Vector3(0, 0, 2), green));

        // Glass sphere
        scene.Add(new Sphere(
            new Vector3(0.35, -0.55, 0.2), 0.45, glass));

        // Silver metallic sphere
        scene.Add(new Sphere(
            new Vector3(-0.35, -0.55, -0.2), 0.45, silver));

        // Area light
        var areaLight = new AreaLight(
            new Vector3(-0.25, 0.999, -0.25),
            new Vector3(0.5, 0, 0),
            new Vector3(0, 0, 0.5),
            new Vector3(15, 15, 15));
        scene.Add(areaLight);
        lights.Add(areaLight);

        var hittable = scene.Build();
        Console.WriteLine($"Scene: {scene.Count} primitives ({hittable.GetType().Name})");

        var camera = new Camera(
            position: new Vector3(0, 0, 3.5),
            lookAt: Vector3.Zero,
            up: Vector3.UnitY,
            vFovDegrees: 40,
            imageWidth: Width,
            imageHeight: Height);

        Render(camera, hittable, lights, spp: SamplesPerPixel, "cornell_classic.ppm");
    }

    /// <summary>
    /// Cornell Box with Stanford bunny mesh. Demonstrates mesh loading,
    /// BVH acceleration and smooth shading.
    /// </summary>
    private static void RenderCornellBunny()
    {
        var scene = new SceneList();
        var lights = new List<ILight>();

        var white = new Lambertian(new Vector3(0.73, 0.73, 0.73));
        var red = new Lambertian(new Vector3(0.65, 0.05, 0.05));
        var green = new Lambertian(new Vector3(0.12, 0.45, 0.15));
        var glass = new Dielectric(Ior: 1.5);

        // Cornell Box walls
        scene.Add(new Quad(new Vector3(-1, -1, -1), new Vector3(2, 0, 0),
                           new Vector3(0, 0, 2), white));
        scene.Add(new Quad(new Vector3(-1, 1, -1), new Vector3(2, 0, 0),
                           new Vector3(0, 0, 2), white));
        scene.Add(new Quad(new Vector3(-1, -1, -1), new Vector3(2, 0, 0),
                           new Vector3(0, 2, 0), white));
        scene.Add(new Quad(new Vector3(-1, -1, -1), new Vector3(0, 2, 0),
                           new Vector3(0, 0, 2), red));
        scene.Add(new Quad(new Vector3(1, -1, -1), new Vector3(0, 2, 0),
                           new Vector3(0, 0, 2), green));

        // Area light
        var areaLight = new AreaLight(
            new Vector3(-0.25, 0.999, -0.25),
            new Vector3(0.5, 0, 0),
            new Vector3(0, 0, 0.5),
            new Vector3(15, 15, 15));
        scene.Add(areaLight);
        lights.Add(areaLight);

        // Glass sphere
        scene.Add(new Sphere(new Vector3(-0.55, -0.55, 0.2), 0.25, glass));

        // Bunny mesh
        var bunnyPath = Path.Combine(AppContext.BaseDirectory, "bunny.obj");
        var bunnyMat = new Lambertian(new Vector3(0.8, 0.7, 0.6));
        Console.WriteLine("Loading bunny mesh...");
        var bunnyMesh = Mesh.Load(bunnyPath, bunnyMat, smoothNormals: false);
        Console.WriteLine($"Bunny loaded: {bunnyMesh.TriangleCount} triangles");

        const double scale = 5.0;
        const double translateY = -1.0 - (0.0332 * scale);
        var bunnyTransform =
            Matrix4x4d.Translation(0.3, translateY, 0) *
            Matrix4x4d.RotationY(25) *
            Matrix4x4d.Scale(scale);
        scene.Add(new Transform(bunnyMesh, bunnyTransform));

        var hittable = scene.Build();
        Console.WriteLine($"Scene: {scene.Count} primitives ({hittable.GetType().Name})");

        var camera = new Camera(
            new Vector3(0, 0, 3.5), Vector3.Zero,
            Vector3.UnitY, 40, Width, Height);

        Render(camera, hittable, lights, spp: SamplesPerPixel, "cornell_bunny.ppm");
    }

    /// <summary>
    /// Cornell Box with bunny, glass sphere, depth of field and motion blur.
    /// Demonstrates thin-lens DoF and shutter-time motion blur.
    /// </summary>
    private static void RenderCornellDofAndMotionBlur()
    {
        var white = new Lambertian(new Vector3(0.73, 0.73, 0.73));
        var red = new Lambertian(new Vector3(0.65, 0.05, 0.05));
        var green = new Lambertian(new Vector3(0.12, 0.45, 0.15));
        var glass = new Dielectric(Ior: 1.5);

        // ── DoF scene ────────────────────────────────────────────────────────

        var dofScene = new SceneList();
        var lights = new List<ILight>();

        dofScene.Add(new Quad(new Vector3(-1, -1, -1), new Vector3(2, 0, 0),
                              new Vector3(0, 0, 2), white));
        dofScene.Add(new Quad(new Vector3(-1, 1, -1), new Vector3(2, 0, 0),
                              new Vector3(0, 0, 2), white));
        dofScene.Add(new Quad(new Vector3(-1, -1, -1), new Vector3(2, 0, 0),
                              new Vector3(0, 2, 0), white));
        dofScene.Add(new Quad(new Vector3(-1, -1, -1), new Vector3(0, 2, 0),
                              new Vector3(0, 0, 2), red));
        dofScene.Add(new Quad(new Vector3(1, -1, -1), new Vector3(0, 2, 0),
                              new Vector3(0, 0, 2), green));

        var areaLight = new AreaLight(
            new Vector3(-0.25, 0.999, -0.25),
            new Vector3(0.5, 0, 0),
            new Vector3(0, 0, 0.5),
            new Vector3(15, 15, 15));
        dofScene.Add(areaLight);
        lights.Add(areaLight);

        dofScene.Add(new Sphere(new Vector3(-0.4, -0.55, 0.5), 0.4, glass));

        var bunnyPath = Path.Combine(AppContext.BaseDirectory, "bunny.obj");
        var bunnyMat = new Lambertian(new Vector3(0.8, 0.7, 0.6));
        Console.WriteLine("Loading bunny...");
        var bunnyMesh = Mesh.Load(bunnyPath, bunnyMat, smoothNormals: false);
        Console.WriteLine($"Loaded: {bunnyMesh.TriangleCount} triangles");

        const double scale = 3.9;
        const double translateY = -1.0 - (0.0332 * scale);
        var bunnyTransform =
            Matrix4x4d.Translation(0.35, translateY, -0.3) *
            Matrix4x4d.RotationY(25) *
            Matrix4x4d.Scale(scale);
        dofScene.Add(new Transform(bunnyMesh, bunnyTransform));

        var dofHittable = dofScene.Build();
        Console.WriteLine($"Scene: {dofScene.Count} primitives ({dofHittable.GetType().Name})");

        var dofCamera = new Camera(
            position: new Vector3(0, 0, 3.5),
            lookAt: Vector3.Zero,
            up: Vector3.UnitY,
            vFovDegrees: 40,
            imageWidth: Width,
            imageHeight: Height,
            aperture: 0.15,
            focusDistance: 3.8);

        Render(dofCamera, dofHittable, lights, spp: SamplesPerPixel, "cornell_dof.ppm");

        // ── Motion blur scene ────────────────────────────────────────────────

        var motionScene = new SceneList();
        var motionLights = new List<ILight>();

        motionScene.Add(new Quad(new Vector3(-1, -1, -1), new Vector3(2, 0, 0),
                                 new Vector3(0, 0, 2), white));
        motionScene.Add(new Quad(new Vector3(-1, 1, -1), new Vector3(2, 0, 0),
                                 new Vector3(0, 0, 2), white));
        motionScene.Add(new Quad(new Vector3(-1, -1, -1), new Vector3(2, 0, 0),
                                 new Vector3(0, 2, 0), white));
        motionScene.Add(new Quad(new Vector3(-1, -1, -1), new Vector3(0, 2, 0),
                                 new Vector3(0, 0, 2), red));
        motionScene.Add(new Quad(new Vector3(1, -1, -1), new Vector3(0, 2, 0),
                                 new Vector3(0, 0, 2), green));

        var motionLight = new AreaLight(
            new Vector3(-0.25, 0.999, -0.25),
            new Vector3(0.5, 0, 0),
            new Vector3(0, 0, 0.5),
            new Vector3(15, 15, 15));
        motionScene.Add(motionLight);
        motionLights.Add(motionLight);

        motionScene.Add(new MovingSphere(
            centre0: new Vector3(-0.3, -0.75, 0.0),
            centre1: new Vector3(0.1, -0.75, 0.0),
            time0: 0.0,
            time1: 1.0,
            radius: 0.25,
            material: new Lambertian(new Vector3(0.8, 0.3, 0.1))));

        var motionHittable = motionScene.Build();

        var motionCamera = new Camera(
            position: new Vector3(0, 0, 3.5),
            lookAt: Vector3.Zero,
            up: Vector3.UnitY,
            vFovDegrees: 40,
            imageWidth: Width,
            imageHeight: Height,
            shutterOpen: 0.0,
            shutterClose: 0.5);

        Render(motionCamera, motionHittable, motionLights, spp: SamplesPerPixel, "cornell_motion.ppm");
    }

    /// <summary>
    /// Shared render helper — sets up the render loop and writes the output.
    /// </summary>
    private static void Render(Camera camera, IHittable scene,
                               IReadOnlyList<ILight> lights,
                               int spp, string filename)
    {
        var fb = new FrameBuffer(Width, Height);
        var loop = new RenderLoop();
        var tilesCompleted = 0;
        var totalTiles = (int)(Math.Ceiling(Width / 16.0) *
                               Math.Ceiling(Height / 16.0));
        var mis = new MisIntegrator { BackgroundRadiance = Vector3.Zero };

        Console.WriteLine($"\nRendering {filename} ({spp} spp)...");
        var sw = System.Diagnostics.Stopwatch.StartNew();

        loop.Render(camera, fb,
            (ray, sampler) => mis.Trace(ray, scene, lights, sampler),
            spp,
            onTileComplete: () =>
            {
                var n = Interlocked.Increment(ref tilesCompleted);
                if (n % 16 == 0 || n == totalTiles)
                    Console.Write($"\r  {n}/{totalTiles} tiles   ");
            });

        sw.Stop();
        Console.WriteLine($"\nDone in {sw.Elapsed.TotalSeconds:F1}s");
        PpmWriter.Write(fb, filename);
        Console.WriteLine($"Saved -> {filename}");
    }
}