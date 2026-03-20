using Core;
using Core.Materials;

namespace Render;

internal class Program
{
    static void Main(string[] args)
    {
        // ── Scene ─────────────────────────────────────────────────────────────────────

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

        // Glass sphere on the left — closer to camera
        scene.Add(new Sphere(new Vector3(-0.4, -0.55, 0.5), 0.4, glass));

        // Bunny on the right — further from camera
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

        scene.Add(new Transform(bunnyMesh, bunnyTransform));

        // Moving sphere for motion blur demo
        var movingSphere = new MovingSphere(
            centre0: new Vector3(-0.3, -0.75, 0.0),
            centre1: new Vector3(0.1, -0.75, 0.0),
            time0: 0.0,
            time1: 1.0,
            radius: 0.25,
            material: new Lambertian(new Vector3(0.8, 0.3, 0.1)));

        var hittable = scene.Build();
        Console.WriteLine($"Scene: {scene.Count} primitives ({hittable.GetType().Name})");

        // ── Render helper ─────────────────────────────────────────────────────────────

        const int width = 512;
        const int height = 512;

        void Render(Camera camera, IHittable sceneRoot,
                    IReadOnlyList<ILight> lightList,
                    int spp, string filename)
        {
            var fb = new FrameBuffer(width, height);
            var loop = new RenderLoop();
            var tilesCompleted = 0;
            var totalTiles = (int)(Math.Ceiling(width / 16.0) *
                                       Math.Ceiling(height / 16.0));
            var mis = new MisIntegrator { BackgroundRadiance = Vector3.Zero };

            Console.WriteLine($"\nRendering {filename} ({spp} spp)...");
            var sw = System.Diagnostics.Stopwatch.StartNew();

            loop.Render(camera, fb,
                (ray, sampler) => mis.Trace(ray, sceneRoot, lightList, sampler),
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

        // ── 1. Depth of Field — focused on the bunny ─────────────────────────────────
        // Camera at Z=3.5, bunny at roughly Z=-0.3
        // Focus distance ≈ 3.5 + 0.3 = 3.8, aperture = 0.15 for visible blur

        var dofCamera = new Camera(
            position: new Vector3(0, 0, 3.5),
            lookAt: Vector3.Zero,
            up: Vector3.UnitY,
            vFovDegrees: 40,
            imageWidth: width,
            imageHeight: height,
            aperture: 0.15,
            focusDistance: 3.8);

        Render(dofCamera, hittable, lights, spp: 256, "cornell_dof.ppm");

        // ── 2. Motion Blur — moving sphere with shutter open ─────────────────────────

        var motionScene = new SceneList();
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
        motionScene.Add(movingSphere);

        var motionLights = new List<ILight> { motionLight };
        var motionHittable = motionScene.Build();

        // Shutter open for full motion interval — sphere sweeps across the scene
        var motionCamera = new Camera(
            position: new Vector3(0, 0, 3.5),
            lookAt: Vector3.Zero,
            up: Vector3.UnitY,
            vFovDegrees: 40,
            imageWidth: width,
            imageHeight: height,
            shutterOpen: 0.0,
            shutterClose: 0.5);

        Render(motionCamera, motionHittable, motionLights, spp: 256, "cornell_motion.ppm");

        Console.Write("Press any key to continue...");
        Console.ReadKey();
    }

    /*static void Main(string[] args)
    {
        // ── Scene ─────────────────────────────────────────────────────────────────────

        var scene = new SceneList();
        var lights = new List<ILight>();

        var white = new Lambertian(new Vector3(0.73, 0.73, 0.73));
        var red = new Lambertian(new Vector3(0.65, 0.05, 0.05));
        var green = new Lambertian(new Vector3(0.12, 0.45, 0.15));
        var glass = new Dielectric(Ior: 1.5);
        var silver = new GgxMetal(
            F0: new Vector3(0.95, 0.93, 0.88),
            Roughness: 0.1);

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
        scene.Add(new Sphere(
            new Vector3(-0.55, -0.55, 0.2), 0.25, glass));

        // ── Load bunny ────────────────────────────────────────────────────────────────

        var bunnyPath = Path.Combine(AppContext.BaseDirectory, "bunny.obj");
        var bunnyMat = new Lambertian(new Vector3(0.8, 0.7, 0.6)); // warm off-white

        Console.WriteLine("Loading bunny mesh...");
        var bunnyMesh = Mesh.Load(bunnyPath, bunnyMat, smoothNormals: false);
        Console.WriteLine($"Bunny loaded: {bunnyMesh.TriangleCount} triangles");

        // Scale to fit Cornell Box, translate to sit on floor (y=-1)
        const double scale = 5.0; //3.9;
        const double translateY = -1.0 - (0.0332 * scale);

        var bunnyTransform =
            Matrix4x4d.Translation(0.3, translateY, 0) *  // shift right, sit on floor
            Matrix4x4d.RotationY(25) *  // slight rotation for interest
            Matrix4x4d.Scale(scale);

        scene.Add(new Transform(bunnyMesh, bunnyTransform));

        // ── Build ─────────────────────────────────────────────────────────────────────

        var hittable = scene.Build();
        Console.WriteLine($"Scene: {scene.Count} top-level primitives, " +
                          $"built as {hittable.GetType().Name}");

        // ── Camera ────────────────────────────────────────────────────────────────────

        const int width = 512;
        const int height = 512;

        var camera = new Camera(
            new Vector3(0, 0, 3.5), Vector3.Zero,
            Vector3.UnitY, 40, width, height);

        // ── Render ────────────────────────────────────────────────────────────────────

        const int samplesPerPixel = 64;

        var fb = new FrameBuffer(width, height);
        var loop = new RenderLoop();
        var tilesCompleted = 0;
        var totalTiles = (int)(Math.Ceiling(width / 16.0) *
                                   Math.Ceiling(height / 16.0));

        var mis = new MisIntegrator { BackgroundRadiance = Vector3.Zero };

        Console.WriteLine($"Rendering {width}×{height} at {samplesPerPixel} spp " +
                          $"on {Environment.ProcessorCount} cores...");

        var sw = System.Diagnostics.Stopwatch.StartNew();

        loop.Render(camera, fb,
            (ray, sampler) => mis.Trace(ray, hittable, lights, sampler),
            samplesPerPixel,
            onTileComplete: () =>
            {
                var n = Interlocked.Increment(ref tilesCompleted);
                if (n % 16 == 0 || n == totalTiles)
                    Console.Write($"\r  {n}/{totalTiles} tiles   ");
            });

        sw.Stop();
        Console.WriteLine($"\nDone in {sw.Elapsed.TotalSeconds:F1}s");

        PpmWriter.Write(fb, "bunny_mesh.ppm");
        Console.WriteLine("Saved -> bunny_mesh.ppm");

        Console.Write("Press any key to continue...");
        Console.ReadKey();
    }*/

    /*static void Main(string[] args)
    {
        // ── Scene ─────────────────────────────────────────────────────────────────────

        var scene = new SceneList();
        var lights = new List<ILight>();

        // Materials
        var white = new Lambertian(new Vector3(0.73, 0.73, 0.73));
        var red = new Lambertian(new Vector3(0.65, 0.05, 0.05));
        var green = new Lambertian(new Vector3(0.12, 0.45, 0.15));
        //var light = new Emissive(new Vector3(15, 15, 15));
        var glass = new Dielectric(Ior: 1.5);
        var silver = new GgxMetal(
            F0: new Vector3(0.95, 0.93, 0.88),
            Roughness: 0.05);

        // Cornell Box walls (unit box from -1 to +1 on X/Y, 0 to -2 on Z)
        //
        //        Ceiling y=+1
        //        Floor   y=-1
        //        Back    z=-1
        //        Left    x=-1  (red)
        //        Right   x=+1  (green)

        // Floor
        scene.Add(new Quad(
            new Vector3(-1, -1, -1),
            new Vector3(2, 0, 0),
            new Vector3(0, 0, 2), white));

        // Ceiling
        scene.Add(new Quad(
            new Vector3(-1, 1, -1),
            new Vector3(2, 0, 0),
            new Vector3(0, 0, 2), white));

        // Back wall
        scene.Add(new Quad(
            new Vector3(-1, -1, -1),
            new Vector3(2, 0, 0),
            new Vector3(0, 2, 0), white));

        // Left wall (red)
        scene.Add(new Quad(
            new Vector3(-1, -1, -1),
            new Vector3(0, 2, 0),
            new Vector3(0, 0, 2), red));

        // Right wall (green)
        scene.Add(new Quad(
            new Vector3(1, -1, -1),
            new Vector3(0, 2, 0),
            new Vector3(0, 0, 2), green));

        // Glass sphere
        scene.Add(new Sphere(
            new Vector3(0.35, -0.55, 0.2), 0.45, glass));

        // Silver metallic sphere
        scene.Add(new Sphere(
            new Vector3(-0.35, -0.55, -0.2), 0.45, silver));

        // Area light (inset rectangle on ceiling)
        var areaLight = new AreaLight(
            new Vector3(-0.25, 0.999, -0.25),
            new Vector3(0.5, 0, 0),
            new Vector3(0, 0, 0.5),
            new Vector3(15, 15, 15));
        scene.Add(areaLight);
        lights.Add(areaLight);

        // ── Build ─────────────────────────────────────────────────────────────────-───

        // 8 primitives — stays as SceneList (below BVH threshold of 16)
        var hittable = scene.Build(); // SceneList for small scenes, BVH for large
        Console.WriteLine($"Scene built: {scene.Count} primitives " +
                  $"({hittable.GetType().Name})");

        // ── Camera ────────────────────────────────────────────────────────────────────

        const int width = 512;
        const int height = 512;

        var camera = new Camera(
            position: new Vector3(0, 0, 3.5),
            lookAt: Vector3.Zero,
            up: Vector3.UnitY,
            vFovDegrees: 40,
            imageWidth: width,
            imageHeight: height);

        // ── Render helper ─────────────────────────────────────────────────────────────

        void Render(Func<Ray, Sampler, Vector3> traceFunc, int spp, string filename)
        {
            var fb = new FrameBuffer(width, height);
            var loop = new RenderLoop();
            var tilesCompleted = 0;
            var totalTiles = (int)(Math.Ceiling(width / 16.0) *
                                       Math.Ceiling(height / 16.0));

            Console.WriteLine($"\nRendering {filename} ({spp} spp)...");
            var sw = System.Diagnostics.Stopwatch.StartNew();

            loop.Render(camera, fb, traceFunc, spp,
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

        // ── Compare at x spp — noise difference is very visible at low spp ───────────

        const int spp = 64;

        Console.WriteLine($"Rendering {width}×{height} at {spp} spp " +
                    $"on {Environment.ProcessorCount} cores...");

        //var brdf = new PathIntegrator { BackgroundRadiance = Vector3.Zero };
        //Render((ray, sampler) => brdf.Trace(ray, hittable, sampler),
        //       spp, "cornell_brdf.ppm");

        var mis = new MisIntegrator { BackgroundRadiance = Vector3.Zero };
        Render((ray, sampler) => mis.Trace(ray, hittable, lights, sampler),
               spp, "cornell_mis.ppm");

        Console.Write("Press any key to continue...");
        Console.ReadKey();
    }*/
}