
namespace Core.Rendering.SPPM;

public sealed class SppmConfig
{
    public int PhotonCount { get; set; } = 500000;
    public float InitialRadius { get; set; } = 0.5f;
    public float Alpha { get; set; } = 0.7f;
    public int MaxCameraDepth { get; set; } = 12;
    public int MaxPhotonDepth { get; set; } = 12;
    public bool EnableDebugImages { get; set; } = true;
}
