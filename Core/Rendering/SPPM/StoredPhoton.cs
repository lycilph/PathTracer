using Core.Math;

namespace Core.Rendering.Sppm;

/// <summary>
/// A photon that has been stored at a diffuse-surface bounce during the photon pass.
/// The hash grid holds arrays of these.
/// </summary>
public readonly struct StoredPhoton
{
    /// <summary>World-space position of the surface bounce.</summary>
    public readonly Vec3 Position;

    /// <summary>
    /// Direction the photon was TRAVELLING when it hit this surface
    /// (i.e. pointing FROM the previous vertex TOWARD this surface).
    /// When evaluating the BSDF at a hit point, use -Wi as the incoming direction.
    /// </summary>
    public readonly Vec3 Wi;

    /// <summary>
    /// Total light power carried by this photon at this bounce, after all
    /// previous BSDF × |cosθ| / pdf multiplications along its path.
    /// Units: total-emitter-flux (NOT divided by photon count M).
    /// The SPPM estimator divides by total_photons_emitted at reconstruction.
    /// </summary>
    public readonly Vec3 Power;

    public StoredPhoton(in Vec3 position, in Vec3 wi, in Vec3 power)
    {
        Position = position;
        Wi       = wi;
        Power    = power;
    }
}