using System.Diagnostics;
using Core.Camera;
using Core.Lights;
using Core.Materials;
using Core.Math;
using Core.Rendering;
using Core.Scene;

namespace CLI;

internal class Program
{
    static void Main(string[] args)
    {
        // Milestone 5 CLI entry point.
        // Supports Cornell box with BVH and an optional cube mesh.
        // Usage:
        //   dotnet run --project src/Tracer.Cli -- <width> <height> <spp> <out.ppm>

        int width = 400;
        int height = 400;
        int spp = 50;
        string outPath = "milestone5.ppm";

        if (args.Length >= 3)
        {
            int.TryParse(args[0], out width);
            int.TryParse(args[1], out height);
            int.TryParse(args[2], out spp);
        }
        if (args.Length >= 4) outPath = args[3];

        float aspect = (float)width / height;

        var red = new Lambertian(new Vec3(0.65f, 0.05f, 0.05f));
        var green = new Lambertian(new Vec3(0.12f, 0.45f, 0.15f));
        var white = new Lambertian(new Vec3(0.73f, 0.73f, 0.73f));
        var lightMat = new DiffuseLight(new Vec3(15f, 15f, 15f));

        var worldList = new HittableList();
        worldList.Add(new YZRect(0, 555, 0, 555, 555, green));
        worldList.Add(new YZRect(0, 555, 0, 555, 0, red));
        worldList.Add(new XZRect(0, 555, 0, 555, 0, white));
        worldList.Add(new XZRect(0, 555, 0, 555, 555, white));
        worldList.Add(new XYRect(0, 555, 0, 555, 555, white));
        worldList.Add(new FlipFace(new XZRect(213, 343, 227, 332, 554, lightMat)));

        // Replace one inner box with a triangle mesh cube (unit cube scaled and translated)
        var cube = TriangleMesh.CreateUnitCube(white);
        IHittable cubePlaced = new Translate(
            new Scale(cube, 165f),
            new Vec3(190f, 82.5f, 150f));
        worldList.Add(cubePlaced);

        // Keep the original boxes for now (still helpful for scenes)
        //worldList.Add(new Box(new Vec3(130, 0, 65), new Vec3(295, 165, 230), white));
        worldList.Add(new Box(new Vec3(265, 0, 295), new Vec3(430, 330, 460), white));

        // Build BVH over the whole world for speed
        var world = new BvhNode(worldList.Objects);

        var lights = new List<ILight>
        {
            new RectAreaLightXZ(213, 343, 227, 332, 554, normal: -Vec3.UnitY, radiance: new Vec3(15f,15f,15f))
        };
        var scene = new Scene(world, lights);

        var camera = new PinholeCamera(
            vfovDegrees: 40f,
            aspectRatio: aspect,
            lookFrom: new Vec3(278f, 278f, -800f),
            lookAt: new Vec3(278f, 278f, 0f),
            vUp: Vec3.UnitY);

        Console.WriteLine($"Rendering (BVH) {width}x{height}, spp={spp} -> {outPath}");
        var sw = Stopwatch.StartNew();
        int last = -1;
        void Report(int done, int total)
        {
            int percent = (int)(100.0 * done / total);
            if (percent == last) return;
            last = percent;
            Console.WriteLine($"Progress: { percent,3}% Rows: { done}/{ total} Elapsed: { sw.Elapsed}");
        }

        var pixels = PathTracer.Render(width, height, spp, maxDepth: 10, camera, scene, baseSeed: 123, reportRowsCompleted: Report);
        Console.WriteLine();
        
        PpmWriter.WriteP6(outPath, width, height, pixels);
        Console.WriteLine($"Done. Elapsed: {sw.Elapsed}");

        Console.Write("Press any key to continue");
        Console.ReadKey();
    }
}
