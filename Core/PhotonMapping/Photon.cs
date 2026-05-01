using Core.Math;

namespace Core.PhotonMapping;

public readonly record struct Photon(
    Vec3 Position,
    Vec3 IncomingDirection, // direction photon arrived from (normalized)
    Vec3 Flux               // carried flux
);