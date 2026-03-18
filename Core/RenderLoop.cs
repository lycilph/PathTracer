namespace Core;

/// <summary>
/// Coordinates parallel path tracing across all pixels of the frame buffer (§2.2).
/// Divides the image into tiles and dispatches each tile to the thread pool.
/// </summary>
public sealed class RenderLoop
{
    /// <summary>Tile width and height in pixels.</summary>
    public int TileSize { get; init; } = 16;

    /// <summary>
    /// Renders the scene into <paramref name="frameBuffer"/>.
    /// The trace function is called once per sample per pixel.
    /// </summary>
    /// <param name="traceFunc">
    /// A function (ray, sampler) → radiance. Called once per sample.
    /// Typically wraps a PathIntegrator or MisIntegrator.
    /// </param>
    public void Render(
        Camera camera,
        FrameBuffer frameBuffer,
        Func<Ray, Sampler, Vector3> traceFunc,
        int samplesPerPixel,
        CancellationToken cancellationToken = default,
        Action? onTileComplete = null)
    {
        var tilesX = (int)Math.Ceiling((double)frameBuffer.Width / TileSize);
        var tilesY = (int)Math.Ceiling((double)frameBuffer.Height / TileSize);

        var tiles = new List<(int tx, int ty)>(tilesX * tilesY);
        for (var ty = 0; ty < tilesY; ty++)
            for (var tx = 0; tx < tilesX; tx++)
                tiles.Add((tx, ty));

        Parallel.ForEach(
            tiles,
            new ParallelOptions { CancellationToken = cancellationToken },
            tile =>
            {
                if (cancellationToken.IsCancellationRequested) return;

                var (tx, ty) = tile;
                var sampler = new Sampler(seed: ty * tilesX + tx);

                RenderTile(tx, ty, camera, frameBuffer,
                           traceFunc, samplesPerPixel, sampler);

                onTileComplete?.Invoke();
            });
    }

    /// <summary>
    /// Renders a single tile — all pixels in the tile, all samples per pixel.
    /// </summary>
    private void RenderTile(
        int tx,
        int ty,
        Camera camera,
        FrameBuffer frameBuffer,
        Func<Ray, Sampler, Vector3> traceFunc,
        int samplesPerPixel,
        Sampler sampler)
    {
        var x0 = tx * TileSize;
        var y0 = ty * TileSize;
        var x1 = Math.Min(x0 + TileSize, frameBuffer.Width);
        var y1 = Math.Min(y0 + TileSize, frameBuffer.Height);

        for (var y = y0; y < y1; y++)
            for (var x = x0; x < x1; x++)
                for (var s = 0; s < samplesPerPixel; s++)
                {
                    var ray = camera.GenerateRay(x, y, sampler.Next(), sampler.Next());
                    var radiance = traceFunc(ray, sampler);
                    frameBuffer.AddSample(x, y, radiance);
                }
    }
}