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
        int width = 400;
        int height = 400;
        int spp = 50;
        string outPath = "milestone4_cornell.ppm";

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

        var world = new HittableList();
        world.Add(new YZRect(0, 555, 0, 555, 555, green));
        world.Add(new YZRect(0, 555, 0, 555, 0, red));
        world.Add(new XZRect(0, 555, 0, 555, 0, white));
        world.Add(new XZRect(0, 555, 0, 555, 555, white));
        world.Add(new XYRect(0, 555, 0, 555, 555, white));

        // Light geometry (flipped so it's visible from inside)
        world.Add(new FlipFace(new XZRect(213, 343, 227, 332, 554, lightMat)));

        world.Add(new Box(new Vec3(130, 0, 65), new Vec3(295, 165, 230), white));
        world.Add(new Box(new Vec3(265, 0, 295), new Vec3(430, 330, 460), white));

        var lights = new List<ILight>
        {
            // Light emitting downward into the box: normal = -Y
            new RectAreaLightXZ(213, 343, 227, 332, 554, normal: -Vec3.UnitY, radiance: new Vec3(15f,15f,15f))
        };

        var scene = new Scene(world, lights);

        var camera = new PinholeCamera(
            vfovDegrees: 40f,
            aspectRatio: aspect,
            lookFrom: new Vec3(278f, 278f, -800f),
            lookAt: new Vec3(278f, 278f, 0f),
            vUp: Vec3.UnitY);

        Console.WriteLine($"Rendering Cornell (NEE+MIS) {width}x{height}, spp={spp} -> {outPath}");
        var pixels = PathTracer.Render(width, height, spp, maxDepth: 10, camera, scene, baseSeed: 123);

        PpmWriter.WriteP6(outPath, width, height, pixels);
        Console.Write("Done... Press any key to continue");
        Console.ReadKey();
    }
}
