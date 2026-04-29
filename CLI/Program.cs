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

        int width = 400;
        int height = 225;
        int spp = 1000;
        string outPath = $"milestone7_microfacet_{spp}spp.ppm";

        if (args.Length >= 3)
        {
            int.TryParse(args[0], out width);
            int.TryParse(args[1], out height);
            int.TryParse(args[2], out spp);
        }
        if (args.Length >= 4) outPath = args[3];

        float aspect = (float)width / height;

        // Materials
        var ground = new Lambertian(new Vec3(0.75f, 0.75f, 0.75f));
        var lightMat = new DiffuseLight(new Vec3(20f, 20f, 20f));

        // Copper-ish / gold-ish F0 values are plausible for demo; we keep it simple.
        var metalRough = new MicrofacetMetal(new Vec3(0.95f, 0.64f, 0.54f), roughness: 0.6f);
        var metalMid = new MicrofacetMetal(new Vec3(0.95f, 0.64f, 0.54f), roughness: 0.25f);
        var metalSharp = new MicrofacetMetal(new Vec3(0.95f, 0.64f, 0.54f), roughness: 0.05f);

        // Scene geometry
        var list = new HittableList();

        // Ground plane as a big XZRect at y=0
        list.Add(new XZRect(-10, 10, -10, 10, k: 0, ground));

        // Area light rectangle above the spheres (XZ plane at y=5), emitting downward (use FlipFace)
        list.Add(new FlipFace(new XZRect(-2, 2, -2, 2, k: 5f, lightMat)));

        list.Add(new Sphere(new Vec3(-1.5f, 1f, -3f), 1f, metalRough));
        list.Add(new Sphere(new Vec3(0.0f, 1f, -3f), 1f, metalMid));
        list.Add(new Sphere(new Vec3(1.5f, 1f, -3f), 1f, metalSharp));

        var world = new BvhNode(list.Objects);

        var lights = new List<ILight>
        {
            // Light aligned with XZ plane at y=5, normal = -Y, radiance matches the emissive material above
            new RectAreaLightXZ(-2, 2, -2, 2, k: 5f, normal: -Vec3.UnitY, radiance: new Vec3(20f,20f,20f))
        };

        var scene = new Scene(world, lights);

        // Camera
        var camera = new PinholeCamera(
            vfovDegrees: 40f,
            aspectRatio: aspect,
            lookFrom: new Vec3(0f, 2f, 3f),
            lookAt: new Vec3(0f, 1f, -3f),
            vUp: Vec3.UnitY);

        Console.WriteLine($"Rendering Microfacet (GGX) {width}x{height}, spp={spp} -> {outPath}");
        var sw = Stopwatch.StartNew();
        int last = -1;

        void Report(int done, int total)
        {
            int percent = (int)(100.0 * done / total);
            if (percent == last) return;
            last = percent;
            Console.WriteLine($"Progress: {percent,3}%  Rows: {done}/{total}  Elapsed: {sw.Elapsed}");
        }

        var pixels = PathTracer.Render(width, height, spp, maxDepth: 10, camera, scene, baseSeed: 123, reportRowsCompleted: Report);

        Console.WriteLine();

        PpmWriter.WriteP6(outPath, width, height, pixels);
        Console.WriteLine($"Done. Elapsed: {sw.Elapsed}");

        Console.Write("Press any key to continue");
        Console.ReadKey();
    }
}
