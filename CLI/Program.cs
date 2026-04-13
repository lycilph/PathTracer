using Core.Camera;
using Core.Math;
using Core.Rendering;
using Core.Scene;

namespace CLI;

internal class Program
{
    static void Main(string[] args)
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
}
