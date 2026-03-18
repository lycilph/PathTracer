using Core;
using Core.Materials;

namespace Render;

internal class Program
{
    static void Main(string[] args)
    {
        // ── Scene ─────────────────────────────────────────────────────────────────────

        var scene = new SceneList();

        // Materials
        var white = new Lambertian(new Vector3(0.73, 0.73, 0.73));
        var red = new Lambertian(new Vector3(0.65, 0.05, 0.05));
        var green = new Lambertian(new Vector3(0.12, 0.45, 0.15));
        var light = new Emissive(new Vector3(15, 15, 15));
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

        // Area light (inset rectangle on ceiling)
        scene.Add(new Quad(
            new Vector3(-0.25, 0.999, -0.25),
            new Vector3(0.5, 0, 0),
            new Vector3(0, 0, 0.5), light));

        // Glass sphere
        scene.Add(new Sphere(
            new Vector3(0.35, -0.55, 0.2), 0.45, glass));

        // Silver metallic sphere
        scene.Add(new Sphere(
            new Vector3(-0.35, -0.55, -0.2), 0.45, silver));

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

        // ── Render ────────────────────────────────────────────────────────────────────

        var frameBuffer = new FrameBuffer(width, height);
        var integrator = new PathIntegrator
        {
            BackgroundRadiance = Vector3.Zero,
            MinDepth = 3,
            MaxDepth = 50
        };

        var renderLoop = new RenderLoop();
        var samplesPerPixel = 2048;
        var tilesCompleted = 0;
        var totalTiles = (int)(Math.Ceiling(width / 16.0) *
                                     Math.Ceiling(height / 16.0));

        Console.WriteLine($"Rendering {width}×{height} at {samplesPerPixel} spp " +
                          $"on {Environment.ProcessorCount} cores...");

        var sw = System.Diagnostics.Stopwatch.StartNew();

        renderLoop.Render(
            hittable, camera, frameBuffer, integrator,
            samplesPerPixel: samplesPerPixel,
            onTileComplete: () =>
            {
                var n = Interlocked.Increment(ref tilesCompleted);
                if (n % 4 == 0 || n == totalTiles)
                    Console.Write($"\r  {n}/{totalTiles} tiles   ");
            });

        sw.Stop();
        Console.WriteLine($"\nDone in {sw.Elapsed.TotalSeconds:F1}s");

        // ── Save ──────────────────────────────────────────────────────────────────────

        var outputPath = "cornellbox.ppm";
        PpmWriter.Write(frameBuffer, outputPath);
        Console.WriteLine($"Saved -> {outputPath}");
        Console.Write("Press any key to continue...");
        Console.ReadKey();
    }
}
