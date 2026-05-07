using Core.Lights;
using Core.Math;
using Core.Sampling;
using Core.Scene;

namespace Core.Rendering;

/// <summary>
/// Next Event Estimation (light sampling) for direct lighting.
/// This matches the "sample one light, shadow ray, BSDF eval" approach.
/// </summary>
public static class DirectLighting
{
    public static Vec3 EstimateDirect(in HitRecord hit, in Vec3 wo, Scene.Scene scene, Sampler sampler, float time)
    {
        int nLights = scene.Lights.Count;
        if (nLights == 0) return Vec3.Zero;

        // Pick one light uniformly
        int index = (int)(sampler.Next1D() * nLights);
        if (index == nLights) index = nLights - 1;

        ILight light = scene.Lights[index];

        // Sample the chosen light from the shading point
        var ls = light.Sample(hit.Point, sampler);
        if (ls.Pdf <= 0f) return Vec3.Zero;

        float cosSurface = Vec3.Dot(ls.Wi, hit.Normal);
        if (cosSurface <= 0f) return Vec3.Zero;

        // Visibility test
        var shadow = new Ray(hit.Point, ls.Wi, time);
        if (scene.World.Hit(shadow, 0.001f, ls.Distance - 0.001f, out _))
            return Vec3.Zero;

        // Evaluate BSDF
        var f = hit.Material.Evaluate(wo, ls.Wi, hit);
        if (f.NearZero()) return Vec3.Zero;

        // Optional MIS weight (matches your path tracer structure)
        float bsdfPdf = hit.Material.Pdf(wo, ls.Wi, hit);
        float lightPdf = (ls.Pdf / nLights);
        float w = Mis.PowerHeuristic(lightPdf, bsdfPdf);

        // Contribution
        return Vec3.Hadamard(f, ls.Radiance) * (cosSurface * w / lightPdf);
    }
}