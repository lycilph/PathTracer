using Core.Algebra;
using Core.Sampling;
using Engine.Integrators;
using Engine.PhotonMapping.Debug;
using Engine.Rendering;

namespace Engine.PhotonMapping.DebugVisualization;

/// <summary>
/// Renders debug visualizations of the photon map into a FrameBuffer.
/// All modes use the existing photon map — no re-tracing is performed.
/// </summary>
public sealed class PhotonDebugRenderer
{
    /// <summary>
    /// Renders the selected debug view into a new FrameBuffer.
    /// </summary>
    /// <param name="mode">The debug visualization mode.</param>
    /// <param name="photonMap">The photon map from the last render pass.</param>
    /// <param name="pixelStates">
    /// Per-pixel PPM state from the last render pass.
    /// </param>
    /// <param name="scene">The scene description.</param>
    /// <returns>A new FrameBuffer containing the debug visualization.</returns>
    public FrameBuffer Render(
        PhotonDebugMode mode,
        PhotonMap photonMap,
        PixelEstimationState[] pixelStates,
        DebugRenderContext context)
    {
        var width = context.ImageWidth;
        var height = context.ImageHeight;
        var fb = new FrameBuffer(width, height);

        switch (mode)
        {
            case PhotonDebugMode.None:
                break;

            case PhotonDebugMode.PhotonDeposits:
                RenderPhotonDeposits(photonMap, context, fb);
                break;

            case PhotonDebugMode.DensityHeatMap:
                RenderDensityHeatMap(photonMap, pixelStates, context, fb);
                break;

            case PhotonDebugMode.RadiusMap:
                RenderRadiusMap(pixelStates, context, fb);
                break;

            case PhotonDebugMode.IndirectOnly:
                RenderIndirectOnly(photonMap, pixelStates, context, fb);
                break;

            case PhotonDebugMode.DirectOnly:
                RenderDirectOnly(context, fb);
                break;
        }

        return fb;
    }

    // ── Photon deposits ───────────────────────────────────────────────────────

    private static void RenderPhotonDeposits(
        PhotonMap photonMap,
        DebugRenderContext context,
        FrameBuffer fb)
    {
        var allPhotons = photonMap.GetAllPhotons();
        var camera = context.Camera;

        // Splat size in pixels
        const int splatRadius = 2;

        foreach (var photon in allPhotons)
        {
            var screenPos = camera.ProjectToScreen(photon.Position);
            if (screenPos is null) continue;

            var (sx, sy) = screenPos.Value;

            // Color by path type
            var color = photon.PathType switch
            {
                PhotonPathType.Direct => new Vector3(1.0, 1.0, 0.0), // yellow
                PhotonPathType.Caustic => new Vector3(0.0, 1.0, 1.0), // cyan
                PhotonPathType.Indirect => new Vector3(1.0, 0.0, 1.0), // magenta
                _ => Vector3.One
            };

            // Scale brightness by power magnitude
            var powerMag = (photon.Power.X + photon.Power.Y + photon.Power.Z) / 3.0;
            var brightness = Math.Min(powerMag * 100.0, 1.0);
            color = color * brightness;

            // Draw a small splat around the projected position
            for (var dy = -splatRadius; dy <= splatRadius; dy++)
                for (var dx = -splatRadius; dx <= splatRadius; dx++)
                {
                    var px = sx + dx;
                    var py = sy + dy;

                    if (px < 0 || px >= fb.Width ||
                        py < 0 || py >= fb.Height)
                        continue;

                    // Circular splat with soft falloff
                    var dist = Math.Sqrt(dx * dx + dy * dy);
                    if (dist > splatRadius) continue;

                    var falloff = 1.0 - dist / splatRadius;
                    fb.AddSample(px, py, color * falloff);
                }
        }
    }

    // ── Density heat map ──────────────────────────────────────────────────────

    private static void RenderDensityHeatMap(
        PhotonMap photonMap,
        PixelEstimationState[] pixelStates,
        DebugRenderContext context,
        FrameBuffer fb)
    {
        var width = context.ImageWidth;
        var height = context.ImageHeight;

        // Find max density for normalization
        var densities = new double[width * height];
        var maxDensity = 0.0;

        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var idx = y * width + x;
                var state = pixelStates[idx];

                var nearest = photonMap.FindNearest(
                    GetPixelWorldPos(x, y, context),
                    50,
                    state.Radius);

                densities[idx] = nearest.Count;
                if (nearest.Count > maxDensity)
                    maxDensity = nearest.Count;
            }

        if (maxDensity <= 0) return;

        // Map density to heat color
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var normalized = densities[y * width + x] / maxDensity;
                fb.AddSample(x, y, HeatMapColor(normalized));
            }
    }

    // ── Radius map ────────────────────────────────────────────────────────────

    private static void RenderRadiusMap(
        PixelEstimationState[] pixelStates,
        DebugRenderContext context,
        FrameBuffer fb)
    {
        var width = context.ImageWidth;
        var height = context.ImageHeight;

        // Find radius range for normalization
        var minRadius = pixelStates.Min(s => s.Radius);
        var maxRadius = pixelStates.Max(s => s.Radius);
        var range = maxRadius - minRadius;

        if (range < 1e-10) range = 1.0;

        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var idx = y * width + x;
                var normalized = (pixelStates[idx].Radius - minRadius) / range;

                // Blue = small radius (converged), Red = large radius
                var color = new Vector3(normalized, 0, 1.0 - normalized);
                fb.AddSample(x, y, color);
            }
    }

    // ── Indirect only ─────────────────────────────────────────────────────────

    private static void RenderIndirectOnly(
        PhotonMap photonMap,
        PixelEstimationState[] pixelStates,
        DebugRenderContext context,
        FrameBuffer fb)
    {
        var width = context.ImageWidth;
        var height = context.ImageHeight;
        var integrator = new PhotonMapIntegrator(
            context.KNearest,
            context.Alpha)
        {
            BackgroundRadiance = context.BackgroundRadiance
        };

        var estimator = new RadianceEstimator
        {
            KNearest = context.KNearest,
            Alpha = context.Alpha
        };

        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var idx = y * width + x;
                var ray = context.Camera.GenerateRay(x, y);

                var pixelSampler = new Sampler(y * width + x);
                var hit = integrator.FindVisibleDiffusePoint(ray, context.Scene, pixelSampler);

                if (hit is null) continue;

                var state = pixelStates[idx];
                var indirect = estimator.Estimate(hit.Value,
                    photonMap, ref state);

                fb.AddSample(x, y, indirect);
            }
    }

    // ── Direct only ───────────────────────────────────────────────────────────

    private static void RenderDirectOnly(
        DebugRenderContext context,
        FrameBuffer fb)
    {
        var width = context.ImageWidth;
        var height = context.ImageHeight;
        var integrator = new MisIntegrator
        {
            BackgroundRadiance = context.BackgroundRadiance
        };

        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var sampler = new Core.Sampling.Sampler(y * width + x);
                var ray = context.Camera.GenerateRay(x, y,
                    sampler.Next(), sampler.Next(), sampler);

                var radiance = integrator.Trace(
                    ray, context.Scene, context.Lights, sampler);

                fb.AddSample(x, y, radiance);
            }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Approximates the world position for a pixel by ray casting.
    /// Returns Vector3.Zero if the ray misses all geometry.
    /// </summary>
    private static Vector3 GetPixelWorldPos(
        int x, int y, DebugRenderContext context)
    {
        var ray = context.Camera.GenerateRay(x, y);
        if (context.Scene.Hit(ray, out var hit))
            return hit.Point;
        return Vector3.Zero;
    }

    /// <summary>
    /// Maps a normalized value [0,1] to a heat map color.
    /// Black → Blue → Cyan → Green → Yellow → Red → White
    /// </summary>
    private static Vector3 HeatMapColor(double t)
    {
        t = Math.Clamp(t, 0, 1);

        return t switch
        {
            < 0.2 => Lerp(new Vector3(0, 0, 0),
                          new Vector3(0, 0, 1), t / 0.2),
            < 0.4 => Lerp(new Vector3(0, 0, 1),
                          new Vector3(0, 1, 1), (t - 0.2) / 0.2),
            < 0.6 => Lerp(new Vector3(0, 1, 1),
                          new Vector3(0, 1, 0), (t - 0.4) / 0.2),
            < 0.8 => Lerp(new Vector3(0, 1, 0),
                          new Vector3(1, 1, 0), (t - 0.6) / 0.2),
            _ => Lerp(new Vector3(1, 1, 0),
                          new Vector3(1, 0, 0), (t - 0.8) / 0.2)
        };
    }

    private static Vector3 Lerp(Vector3 a, Vector3 b, double t)
        => a + (b - a) * t;
}