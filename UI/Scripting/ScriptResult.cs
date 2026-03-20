using ScriptApi;

namespace UI.Scripting;

/// <summary>
/// The result of compiling and running a scene script.
/// </summary>
public sealed class ScriptResult
{
    /// <summary>True if the script compiled and ran successfully.</summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// The built scene description. Only valid when
    /// <see cref="IsSuccess"/> is true.
    /// </summary>
    public SceneDescription? Scene { get; }

    /// <summary>
    /// Compilation or runtime errors. Only populated when
    /// <see cref="IsSuccess"/> is false.
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    private ScriptResult(bool isSuccess,
                         SceneDescription? scene,
                         IReadOnlyList<string> errors)
    {
        IsSuccess = isSuccess;
        Scene = scene;
        Errors = errors;
    }

    public static ScriptResult Success(SceneDescription scene)
        => new(true, scene, []);

    public static ScriptResult Failure(IReadOnlyList<string> errors)
        => new(false, null, errors);
}