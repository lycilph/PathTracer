using System.Diagnostics;
using Core.Camera;
using Core.Lights;
using Core.Materials;
using Core.Math;
using Core.Rendering;
using Core.Scene;
using Core.Scene.Scenes;

namespace CLI;

internal class Program
{
    static void Main(string[] args)
    {

        int width = 400;
        int height = 400;
        int spp = 500;
        int threads = Environment.ProcessorCount-1;
        string outPath = $"Image_pinhole_{spp}spp.ppm";

        if (args.Length >= 3)
        {
            int.TryParse(args[0], out width);
            int.TryParse(args[1], out height);
            int.TryParse(args[2], out spp);
        }
        if (args.Length >= 4) outPath = args[3];

        //var (scene, camera) = CornellMaterialsShowcase.Create(width, height, tintedGlass: true);

        var useThinLens = false;
        var (scene, pinhole, thin) = ThinLensDofShowcase.Create(width, height);
        var camera = useThinLens ? thin : pinhole;

        Console.WriteLine($"Rendering image {width}x{height}, spp={spp}, threads={threads} -> {outPath}");
        var sw = Stopwatch.StartNew();
        int last = -1;

        void Report(int done, int total)
        {
            int percent = (int)(100.0 * done / total);
            if (percent == last) return;
            last = percent;
            Console.WriteLine($"Progress: {percent,3}%  Tiles: {done}/{total}  Elapsed: {sw.Elapsed}");
        }

        var pixels = PathTracer.Render(
            width, height, spp,
            maxDepth: 12,
            camera, scene,
            baseSeed: 123,
            reportProgress: Report,
            tileSize: 16,
            maxDegreeOfParallelism: threads);

        Console.WriteLine();

        PpmWriter.WriteP6(outPath, width, height, pixels);
        Console.WriteLine($"Done. Elapsed: {sw.Elapsed}");

        Console.Write("Press any key to continue");
        Console.ReadKey();
    }
}
