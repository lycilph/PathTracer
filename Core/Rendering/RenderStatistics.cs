
namespace Core.Rendering;

public sealed class RenderStatistics
{
    public long CameraRays { get; set; }
    public long PhotonRays { get; set; }
    public long PhotonHits { get; set; }
    public int Iteration { get; set; }
    public float CurrentRadius { get; set; }
}
