using Core.Math;

namespace Core.Rendering.Debugging;

public sealed class DebugFrame
{
    public required string Name { get; init; }
    public required Vec3[] Pixels { get; init; }
    public required int Width { get; init; }
    public required int Height { get; init; }
}
