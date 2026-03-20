using Core;
using Core.Acceleration;
using Core.Algebra;
using Core.Geometry;
using Core.Sampling;

namespace Engine.Lighting;

/// <summary>
/// A rectangular area light — both a visible geometric primitive and a
/// samplable light source for MIS direct lighting (§3.9.1).
/// </summary>
public sealed class AreaLight : IHittable, IBoundable, ILight
{
    private readonly Quad _quad;
    private readonly Vector3 _emission;
    private readonly double _area;

    /// <summary>The underlying quad geometry, also added to the scene for visibility.</summary>
    public IHittable Geometry => _quad;

    /// <param name="corner">One corner of the light rectangle in world space.</param>
    /// <param name="edge1">First edge vector in world units.</param>
    /// <param name="edge2">Second edge vector in world units.</param>
    /// <param name="emission">Emitted radiance. HDR values above 1 are valid.</param>
    public AreaLight(Vector3 corner, Vector3 edge1, Vector3 edge2, Vector3 emission)
    {
        _emission = emission;
        _quad = new Quad(corner, edge1, edge2, new Materials.Emissive(emission));
        _area = Vector3.Cross(edge1, edge2).Length;
    }

    // ── IHittable ─────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public bool Hit(Ray ray, out HitRecord hit) => _quad.Hit(ray, out hit);

    // ── IBoundable ────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public Aabb GetBounds() => _quad.GetBounds();

    // ── ILight ────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    /// <remarks>
    /// Samples a uniformly random point on the quad surface.
    /// PDF_area = 1/A converts to solid angle measure via (§3.7.3):
    /// PDF_ω = PDF_area · ‖P_L − x‖² / |cosθ_L|
    /// </remarks>
    public double Sample(Vector3 origin, Sampler sampler,
                         out Vector3 pointOnLight,
                         out Vector3 normal,
                         out Vector3 emission)
    {
        // Uniform point on the parallelogram
        pointOnLight = _quad.Corner
                     + sampler.Next() * _quad.Edge1
                     + sampler.Next() * _quad.Edge2;

        normal = _quad.Normal;
        emission = _emission;

        return Pdf(origin, pointOnLight);
    }

    /// <inheritdoc/>
    public double Pdf(Vector3 origin, Vector3 pointOnLight)
    {
        var toLight = pointOnLight - origin;
        var distSq = toLight.LengthSquared;
        var cosAtLight = Math.Abs(Vector3.Dot(_quad.Normal, (-toLight).Normalize()));

        // Guard against degenerate geometry
        if (cosAtLight < 1e-10)
            return 0;

        // Convert area PDF to solid angle PDF (§3.7.3)
        return distSq / (cosAtLight * _area);
    }
}