using Core.Math;
using Core.Sampling;

namespace Core.Lights;

/// <summary>
/// Optional interface for lights that can emit photons.
/// Not all ILight implementations need to support photon emission,
/// so this is kept separate from ILight.
///
/// Convention: each photon carries the TOTAL emitted power (radiance × π × area for
/// Lambertian emitters). The final SPPM estimate divides by total_photons_emitted,
/// so per-photon power is independent of M. This keeps photon power a physical quantity.
/// </summary>
public interface IPhotonEmitter
{
    /// <summary>
    /// Samples a photon emission event from this light source.
    /// The returned power is the total emitted flux (NOT divided by photon count).
    /// </summary>
    PhotonEmission EmitPhoton(Sampler sampler);
}

public readonly struct PhotonEmission
{
    /// <summary>Origin on the light surface.</summary>
    public readonly Vec3 Position;

    /// <summary>Emission direction, pointing away from the light INTO the scene.</summary>
    public readonly Vec3 Direction;

    /// <summary>
    /// Total emitted power of this light (W per channel in linear RGB).
    /// For a Lambertian area light: Power = Radiance × π × Area.
    /// The SPPM integrator divides by total_photons_emitted at the end.
    /// </summary>
    public readonly Vec3 Power;

    public PhotonEmission(in Vec3 position, in Vec3 direction, in Vec3 power)
        => (Position, Direction, Power) = (position, direction, power);
}
