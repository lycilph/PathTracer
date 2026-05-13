
using Core.Camera;
using Core.Rendering.Debugging;

namespace Core.Rendering;

public interface IProgressiveIntegrator
{
    void Initialize(
        int width,
        int height,
        Core.Scene.Scene scene,
        ICamera camera);

    void RenderIteration(
        AccumulationBuffer accumulation,
        int iteration,
        CancellationToken token);

    IReadOnlyList<DebugFrame> GetDebugFrames();

    RenderStatistics GetStatistics();
}
