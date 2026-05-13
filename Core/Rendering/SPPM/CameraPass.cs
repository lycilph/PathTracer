
using Core.Camera;

namespace Core.Rendering.SPPM;

public static class CameraPass
{
    public static void Execute(
        int width,
        int height,
        Scene.Scene scene,
        ICamera camera,
        VisiblePoint[] visiblePoints,
        SppmPixel[] pixels,
        SppmConfig config,
        CancellationToken token)
    {
        // Placeholder:
        // Integrate existing EvaluateRay traversal here and
        // store visible points at diffuse interactions.
    }
}
