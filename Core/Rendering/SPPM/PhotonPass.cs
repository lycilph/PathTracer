
namespace Core.Rendering.SPPM;

public static class PhotonPass
{
    public static void Execute(
        Scene.Scene scene,
        VisiblePoint[] visiblePoints,
        SppmPixel[] pixels,
        PhotonHashGrid grid,
        SppmConfig config,
        CancellationToken token)
    {
        // Placeholder:
        // Emit photons from scene lights and gather
        // into nearby visible points.
    }
}
