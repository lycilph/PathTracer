namespace Engine.PhotonMapping.Debug;

/// <summary>
/// Debug visualization modes for the photon map.
/// Applied after rendering completes — does not trigger re-tracing.
/// </summary>
public enum PhotonDebugMode
{
    /// <summary>Normal rendering — no debug overlay.</summary>
    None,

    /// <summary>
    /// Renders photon deposit positions as 3D colored dots.
    /// Direct photons = yellow, caustic photons = cyan,
    /// indirect photons = magenta.
    /// </summary>
    PhotonDeposits,

    /// <summary>
    /// Colors each pixel by the number of photons within its
    /// current search radius. Hot colors = high density.
    /// </summary>
    DensityHeatMap,

    /// <summary>
    /// Colors each pixel by its current PPM search radius.
    /// Shows convergence — uniform color means fully converged.
    /// </summary>
    RadiusMap,

    /// <summary>
    /// Shows only the indirect illumination component.
    /// Direct lighting is excluded.
    /// </summary>
    IndirectOnly,

    /// <summary>
    /// Shows only the direct illumination component from MIS.
    /// Photon map contribution is excluded.
    /// </summary>
    DirectOnly
}