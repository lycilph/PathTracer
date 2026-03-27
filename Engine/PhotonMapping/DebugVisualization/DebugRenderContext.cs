using Core;
using Core.Algebra;
using Core.Geometry;
using Engine.Rendering;

namespace Engine.PhotonMapping.DebugVisualization;

/// <summary>
/// Contains everything the <see cref="PhotonDebugRenderer"/> needs
/// to render a debug view, without depending on ScriptApi.
/// </summary>
public sealed class DebugRenderContext
{
    public Camera Camera { get; }
    public IHittable Scene { get; }
    public IReadOnlyList<ILight> Lights { get; }
    public int ImageWidth { get; }
    public int ImageHeight { get; }
    public Vector3 BackgroundRadiance { get; }
    public int KNearest { get; }
    public double Alpha { get; }

    public DebugRenderContext(
        Camera camera,
        IHittable scene,
        IReadOnlyList<ILight> lights,
        int imageWidth,
        int imageHeight,
        Vector3 backgroundRadiance,
        int kNearest,
        double alpha)
    {
        Camera = camera;
        Scene = scene;
        Lights = lights;
        ImageWidth = imageWidth;
        ImageHeight = imageHeight;
        BackgroundRadiance = backgroundRadiance;
        KNearest = kNearest;
        Alpha = alpha;
    }
}