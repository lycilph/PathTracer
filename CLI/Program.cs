using Core.Camera;
using Core.Materials;
using Core.Math;
using Core.Rendering;
using Core.Scene;

namespace CLI;

internal class Program
{
    static void Main(string[] args)
    {
        //1280 720 out.ppm
        //Milestone1(["1280", "720", "out.ppm"]);
        
        //1280 720 100 out.ppm
        Milestone2(["128", "72", "100", "out.ppm"]);
    }

    private static void Milestone1(string[] args)
    {
        // Milestone 1 CLI entry point.
        // Renders a single-sample normal-visualization sphere scene to a PPM file.
        int width = 400;
        int height = 225; // 16:9
        string outPath = "milestone1.ppm";

        if (args.Length >= 2 && int.TryParse(args[0], out var w) && int.TryParse(args[1], out var h))
        {
            width = w;
            height = h;
        }
        if (args.Length >= 3)
            outPath = args[2];

        float aspect = (float)width / height;

        // Scene
        var world = new HittableList();
        world.Add(new Sphere(new Vec3(0f, 0f, -1f), 0.5f));
        world.Add(new Sphere(new Vec3(0f, -100.5f, -1f), 100f)); // ground

        // Camera
        var camera = new PinholeCamera(
            vfovDegrees: 60f,
            aspectRatio: aspect,
            lookFrom: new Vec3(0f, 0.5f, 1.5f),
            lookAt: new Vec3(0f, 0f, -1f),
            vUp: Vec3.UnitY);

        Console.WriteLine($"Rendering {width}x{height} -> {outPath}");
        var pixels = SimpleRenderer.Render(width, height, camera, world);

        PpmWriter.WriteP6(outPath, width, height, pixels);
        Console.Write("Done... Press any key to continue");
        Console.ReadKey();
    }

    private static void Milestone2(string[] args)
    {

        // Milestone 2 CLI entry point.
        // Usage:
        //   dotnet run --project src/Tracer.Cli -- <width> <height> <spp> <out.ppm>
        int width = 400;
        int height = 225;
        int spp = 50;
        string outPath = "milestone2.ppm";

        if (args.Length >= 3)
        {
            int.TryParse(args[0], out width);
            int.TryParse(args[1], out height);
            int.TryParse(args[2], out spp);
        }
        if (args.Length >= 4) outPath = args[3];

        float aspect = (float)width / height;

        var world = new HittableList();
        world.Add(new Sphere(new Vec3(0f, 0f, -1f), 0.5f));
        world.Add(new Sphere(new Vec3(0f, -100.5f, -1f), 100f));

        var camera = new PinholeCamera(60f, aspect, new Vec3(0f, 0.5f, 1.5f), new Vec3(0f, 0f, -1f), Vec3.UnitY);
        var lambert = new Lambertian(new Vec3(0.8f, 0.8f, 0.8f));

        Console.WriteLine($"Rendering {width}x{height}, spp={spp}");
        var pixels = PathTracer.Render(width, height, spp, camera, world, lambert);
        PpmWriter.WriteP6(outPath, width, height, pixels);
        Console.Write("Done... Press any key to continue");
        Console.ReadKey();
    }
}
