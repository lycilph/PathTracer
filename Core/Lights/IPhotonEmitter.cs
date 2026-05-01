using Core.Math;
using Core.Sampling;

namespace Core.Lights;

public interface IPhotonEmitter
{
    // Total emitted power (for choosing lights proportionally)
    Vec3 Power { get; }

    // Emit a photon ray and its initial flux (power carried by the photon)
    void EmitPhoton(Sampler sampler, out Ray ray, out Vec3 flux);
}