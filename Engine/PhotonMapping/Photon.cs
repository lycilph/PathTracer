using Core.Algebra;

namespace Engine.PhotonMapping;

/// <summary>
/// Represents a single photon stored in the photon map.
/// Carries position, incident direction, power and path type.
/// </summary>
public sealed class Photon
{
    /// <summary>World-space position where the photon was deposited.</summary>
    public Vector3 Position { get; }

    /// <summary>
    /// The direction the photon was travelling when it hit the surface.
    /// Points toward the surface (incident direction).
    /// </summary>
    public Vector3 Direction { get; }

    /// <summary>
    /// The power (flux) carried by this photon per RGB channel.
    /// Units: watts per RGB channel.
    /// </summary>
    public Vector3 Power { get; }

    /// <summary>The type of path this photon travelled.</summary>
    public PhotonPathType PathType { get; }

    public Photon(Vector3 position,
                  Vector3 direction,
                  Vector3 power,
                  PhotonPathType pathType)
    {
        Position = position;
        Direction = direction;
        Power = power;
        PathType = pathType;
    }
}

/// <summary>
/// Classifies the light path a photon travelled before being deposited.
/// Used for debug visualization and for separating caustic from indirect
/// contributions.
/// </summary>
public enum PhotonPathType
{
    /// <summary>
    /// Direct photon — emitted from a light and deposited on the first
    /// diffuse surface without any specular bounces.
    /// Not stored in the global photon map — handled by direct lighting.
    /// </summary>
    Direct,

    /// <summary>
    /// Caustic photon — travelled through at least one specular surface
    /// (mirror or glass) before hitting a diffuse surface.
    /// Creates bright caustic patterns under glass objects.
    /// </summary>
    Caustic,

    /// <summary>
    /// Indirect photon — bounced off at least one diffuse surface before
    /// being deposited. Contributes to soft indirect illumination.
    /// </summary>
    Indirect
}