namespace Scripting;

public interface ISceneScriptEngine
{
    CompilationResult TryCompile(string code);
    Task<SceneDefinition> ExecuteAsync(string code, int width, int height, CancellationToken token);
}