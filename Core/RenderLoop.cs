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
    /// Renders the scene into <paramref name="frameBuffer"/> using path tracing.
    /// Blocks until all tiles are complete or cancellation is requested.
    /// </summary>
    /// <param name="scene">The scene to render.</param>
    /// <param name="camera">The camera defining the view.</param>
    /// <param name="frameBuffer">The buffer to accumulate samples into.</param>
    /// <param name="integrator">The path integrator to use per ray.</param>
    /// <param name="samplesPerPixel">Number of samples to trace per pixel.</param>
    /// <param name="cancellationToken">Allows the caller to abort rendering early.</param>
    /// <param name="onTileComplete">
    /// Optional callback invoked after each tile completes — useful for UI progress updates.
    /// </param>
    public void Render(
        IHittable scene,
        Camera camera,
        FrameBuffer frameBuffer,
        PathIntegrator integrator,
        int samplesPerPixel,
        CancellationToken cancellationToken = default,
        Action? onTileComplete = null)
    {
        var tilesX = (int)Math.Ceiling((double)frameBuffer.Width / TileSize);
        var tilesY = (int)Math.Ceiling((double)frameBuffer.Height / TileSize);

        // Build the full list of tiles upfront
        var tiles = new List<(int tx, int ty)>(tilesX * tilesY);
        for (var ty = 0; ty < tilesY; ty++)
            for (var tx = 0; tx < tilesX; tx++)
                tiles.Add((tx, ty));

        // Parallel dispatch — each tile is an independent unit of work
        Parallel.ForEach(
            tiles,
            new ParallelOptions { CancellationToken = cancellationToken },
            tile =>
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                var (tx, ty) = tile;

                // Each tile gets its own seeded sampler — no shared state
                var sampler = new Sampler(seed: ty * tilesX + tx);

                RenderTile(tx, ty, scene, camera, frameBuffer,
                           integrator, samplesPerPixel, sampler);

                onTileComplete?.Invoke();
            });
    }

    /// <summary>
    /// Renders a single tile — all pixels in the tile, all samples per pixel.
    /// </summary>
    private void RenderTile(
        int tx,
        int ty,
        IHittable scene,
        Camera camera,
        FrameBuffer frameBuffer,
        PathIntegrator integrator,
        int samplesPerPixel,
        Sampler sampler)
    {
        var x0 = tx * TileSize;
        var y0 = ty * TileSize;
        var x1 = Math.Min(x0 + TileSize, frameBuffer.Width);
        var y1 = Math.Min(y0 + TileSize, frameBuffer.Height);

        for (var y = y0; y < y1; y++)
            for (var x = x0; x < x1; x++)
            {
                for (var s = 0; s < samplesPerPixel; s++)
                {
                    var ray = camera.GenerateRay(x, y, sampler.Next(), sampler.Next());
                    var radiance = integrator.Trace(ray, scene, sampler);
                    frameBuffer.AddSample(x, y, radiance);
                }
            }
    }
}