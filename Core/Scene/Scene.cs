using Core.Lights;

namespace Core.Scene;

public sealed class Scene
{
    public IHittable World { get; }
    public IReadOnlyList<ILight> Lights { get; }

    public Scene(IHittable world, IReadOnlyList<ILight> lights)
    {
        World = world;
        Lights = lights;
    }
}