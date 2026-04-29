using Core.Camera;
using Core.Lights;
using Core.Materials;
using Core.Math;
using Core.Random;
using Core.Sampling;
using Core.Scene;

namespace Core.Rendering;

public static class PathTracer
{
    public static Vec3[] Render(
        int width,
        int height,
        int samplesPerPixel,
        int maxDepth,
        PinholeCamera camera,
        Scene.Scene scene,
        ulong baseSeed = 1,
        Action<int, int>? reportProgress = null,
        int tileSize = 16,
        int? maxDegreeOfParallelism = null)
    {
        var film = new Vec3[width * height];

        // Build tiles (x0..x1, y0..y1)
        var tiles = BuildTiles(width, height, tileSize);
        int totalTiles = tiles.Count;
        int tilesCompleted = 0;

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = maxDegreeOfParallelism ?? Environment.ProcessorCount
        };

        // Parallelize over tiles
        Parallel.ForEach(tiles, options, tile =>
        {
            for (int y = tile.Y0; y < tile.Y1; y++)
            {
                int rowOffset = y * width;

                for (int x = tile.X0; x < tile.X1; x++)
                {
                    Vec3 sum = Vec3.Zero;

                    // Important: keep sample accumulation order deterministic per pixel
                    for (int s = 0; s < samplesPerPixel; s++)
                    {
                        ulong seed = SeedHash.PixelSampleSeed(x, y, s, baseSeed);
                        var rng = new Pcg32(seed);
                        var sampler = new Sampler(rng);

                        float u = (x + sampler.Next1D()) / width;
                        float v = (y + sampler.Next1D()) / height;
                        var ray = camera.GetRay(u, 1f - v);

                        sum += Li(ray, scene, sampler, maxDepth, bounce: 0, mediumSigmaA: Vec3.Zero);
                    }

                    film[rowOffset + x] = sum / samplesPerPixel;
                }
            }

            int done = Interlocked.Increment(ref tilesCompleted);
            reportProgress?.Invoke(done, totalTiles);
        });

        return film;
    }

    private readonly struct Tile
    {
        public readonly int X0, X1, Y0, Y1;
        public Tile(int x0, int x1, int y0, int y1) => (X0, X1, Y0, Y1) = (x0, x1, y0, y1);
    }

    private static List<Tile> BuildTiles(int width, int height, int tileSize)
    {
        if (tileSize <= 0) tileSize = 16;

        var tiles = new System.Collections.Generic.List<Tile>(
            ((width + tileSize - 1) / tileSize) * ((height + tileSize - 1) / tileSize));

        for (int y0 = 0; y0 < height; y0 += tileSize)
        {
            int y1 = System.Math.Min(y0 + tileSize, height);

            for (int x0 = 0; x0 < width; x0 += tileSize)
            {
                int x1 = System.Math.Min(x0 + tileSize, width);
                tiles.Add(new Tile(x0, x1, y0, y1));
            }
        }

        return tiles;
    }

    private static Vec3 Li(in Ray ray, Scene.Scene scene, Sampler sampler, int depth, int bounce, in Vec3 mediumSigmaA)
    {
        if (depth <= 0)
            return Vec3.Zero;

        if (!scene.World.Hit(ray, 0.001f, float.PositiveInfinity, out var hit))
            return Vec3.Zero;

        // Apply medium transmittance along the ray segment up to the hit point
        Vec3 transmittance = MediumTransmittance(mediumSigmaA, hit.T);

        var mat = hit.Material;
        Vec3 wo = (-ray.Direction).Normalized();

        Vec3 L = mat.Emitted(ray, hit);

        // NEE only for non-delta
        if (!mat.IsDelta && scene.Lights.Count > 0)
            L += EstimateDirect(hit, wo, scene, sampler, ray.Time);

        if (depth == 1)
            return Vec3.Hadamard(transmittance, L);

        if (!mat.Sample(wo, hit, sampler, out var wi, out var bsdfPdf, out var f))
            return Vec3.Hadamard(transmittance, L);

        float cos = Vec3.Dot(wi, hit.Normal);
        float absCos = float.Abs(cos);

        if (!mat.IsDelta)
        {
            if (bsdfPdf <= 0f || cos <= 0f) return Vec3.Hadamard(transmittance, L);
        }
        else
        {
            if (bsdfPdf <= 0f || absCos <= 0f) return Vec3.Hadamard(transmittance, L);
        }

        Vec3 throughput = f * (absCos / bsdfPdf);

        // Russian roulette after a few bounces
        const int rrStartBounce = 3;
        if (bounce >= rrStartBounce)
        {
            float p = RussianRoulette.ContinuationProbability(throughput);
            if (sampler.Next1D() > p)
                return Vec3.Hadamard(transmittance, L);

            throughput = throughput / p;
        }

        // Update medium absorption if we crossed a dielectric boundary via refraction
        Vec3 nextMediumSigmaA = mediumSigmaA;
        if (mat is Dielectric glass)
        {
            // Refraction produces direction on the other side of the surface normal
            bool transmitted = Vec3.Dot(wi, hit.Normal) < 0f;

            if (transmitted)
            {
                // Entering if front face, exiting if back face
                nextMediumSigmaA = hit.FrontFace ? glass.SigmaA : Vec3.Zero;
            }
        }

        var scattered = new Ray(hit.Point, wi, ray.Time);

        // If the BSDF-sampled ray hits a light:
        if (scene.World.Hit(scattered, 0.001f, float.PositiveInfinity, out var hit2))
        {
            Vec3 Le = hit2.Material.Emitted(scattered, hit2);
            if (!Le.NearZero())
            {
                if (!mat.IsDelta)
                {
                    float lightPdf = SceneLightPdf(scene, hit.Point, wi);
                    float w = Mis.PowerHeuristic(bsdfPdf, lightPdf);
                    L += Vec3.Hadamard(throughput, Le) * w;
                }
                else
                {
                    // Delta: no MIS
                    L += Vec3.Hadamard(throughput, Le);
                }

                return Vec3.Hadamard(transmittance, L);
            }
        }

        L += Vec3.Hadamard(throughput, Li(scattered, scene, sampler, depth - 1, bounce + 1, nextMediumSigmaA));
        return Vec3.Hadamard(transmittance, L);
    }

    private static Vec3 MediumTransmittance(in Vec3 sigmaA, float distance)
    {
        if (sigmaA.NearZero() || distance <= 0f)
            return Vec3.One;

        return new Vec3(
            float.Exp(-sigmaA.X * distance),
            float.Exp(-sigmaA.Y * distance),
            float.Exp(-sigmaA.Z * distance));
    }

    private static Vec3 EstimateDirect(in HitRecord hit, in Vec3 wo, Scene.Scene scene, Sampler sampler, float time)
    {
        int nLights = scene.Lights.Count;
        if (nLights == 0) return Vec3.Zero;

        int index = (int)(sampler.Next1D() * nLights);
        if (index == nLights) index = nLights - 1;

        ILight light = scene.Lights[index];
        var ls = light.Sample(hit.Point, sampler);
        if (ls.Pdf <= 0f) return Vec3.Zero;

        float cosSurface = Vec3.Dot(ls.Wi, hit.Normal);
        if (cosSurface <= 0f) return Vec3.Zero;

        var shadow = new Ray(hit.Point, ls.Wi, time);
        if (scene.World.Hit(shadow, 0.001f, ls.Distance - 0.001f, out _))
            return Vec3.Zero;

        var f = hit.Material.Evaluate(wo, ls.Wi, hit);
        float bsdfPdf = hit.Material.Pdf(wo, ls.Wi, hit);

        float lightPdf = ls.Pdf / nLights;
        float w = Mis.PowerHeuristic(lightPdf, bsdfPdf);

        return Vec3.Hadamard(f, ls.Radiance) * (cosSurface * w / lightPdf);
    }

    private static float SceneLightPdf(Scene.Scene scene, in Vec3 refPoint, in Vec3 wi)
    {
        int nLights = scene.Lights.Count;
        if (nLights == 0) return 0f;

        float sum = 0f;
        for (int i = 0; i < nLights; i++)
            sum += scene.Lights[i].Pdf(refPoint, wi);

        return sum / nLights;
    }
}