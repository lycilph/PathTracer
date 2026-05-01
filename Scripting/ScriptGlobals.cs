namespace Scripting;

public sealed class ScriptGlobals
{
    public int Width { get; }
    public int Height { get; }

    public SceneApi Scene { get; }

    public ScriptGlobals(int width, int height)
    {
        Width = width;
        Height = height;
        Scene = new SceneApi(width, height);
    }
}