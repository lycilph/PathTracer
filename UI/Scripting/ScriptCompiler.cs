using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using ScriptApi;

namespace UI.Scripting;

/// <summary>
/// Compiles and executes C# scene scripts using Roslyn.
/// Each execution runs in an isolated AssemblyLoadContext that is
/// unloaded after the script completes.
/// </summary>
public sealed class ScriptCompiler
{
    /// <summary>
    /// Usings automatically injected into every script so the user
    /// does not need to declare them manually.
    /// </summary>
    private static readonly IReadOnlyList<string> InjectedUsings =
    [
        "System",
        "System.Collections.Generic",
        "Core.Algebra",
        "Core.Geometry",
        "Engine.Materials",
        "Engine.Lighting",
        "ScriptApi"
    ];

    /// <summary>
    /// Compiles and runs a scene script, returning the
    /// <see cref="SceneDescription"/> it produces.
    /// </summary>
    /// <param name="scriptText">The C# script text to compile and run.</param>
    /// <param name="cancellationToken">Token to cancel compilation.</param>
    /// <returns>A <see cref="ScriptResult"/> containing either the
    /// built scene or a list of compilation/runtime errors.</returns>
    public async Task<ScriptResult> CompileAndRunAsync(
        string scriptText,
        CancellationToken cancellationToken = default)
    {
        var context = new ScriptAssemblyLoadContext();

        try
        {
            var options = BuildScriptOptions(context);
            var fullScript = InjectUsings(scriptText);

            var result = await CSharpScript
                .RunAsync<SceneDescription>(
                    fullScript,
                    options,
                    cancellationToken: cancellationToken);

            if (result.ReturnValue is null)
                return ScriptResult.Failure(
                    ["Script did not return a SceneDescription. " +
                     "Make sure your script ends with a return statement."]);

            return ScriptResult.Success(result.ReturnValue);
        }
        catch (CompilationErrorException ex)
        {
            var errors = ex.Diagnostics
                .Select(d => d.ToString())
                .ToList();
            return ScriptResult.Failure(errors);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return ScriptResult.Failure(
                [$"Runtime error: {ex.Message}"]);
        }
        finally
        {
            context.Unload();
        }
    }

    private static ScriptOptions BuildScriptOptions(
        AssemblyLoadContext context)
    {
        // Collect all assemblies needed by the script
        var assemblies = new[]
        {
            typeof(object).Assembly,                          // System
            typeof(Core.Algebra.Vector3).Assembly,            // Core
            typeof(Engine.Materials.Lambertian).Assembly,     // Engine
            typeof(SceneDescription).Assembly,                // ScriptApi
        };

        return ScriptOptions.Default
            .WithReferences(assemblies)
            .WithImports(InjectedUsings);
    }

    private static string InjectUsings(string scriptText)
    {
        var usings = string.Join(
            Environment.NewLine,
            InjectedUsings.Select(u => $"using {u};"));

        return $"{usings}{Environment.NewLine}{Environment.NewLine}{scriptText}";
    }
}

/// <summary>
/// Isolated assembly load context for script execution.
/// Unloaded after each script run to prevent memory leaks.
/// </summary>
internal sealed class ScriptAssemblyLoadContext : AssemblyLoadContext
{
    public ScriptAssemblyLoadContext()
        : base(isCollectible: true) { }

    protected override Assembly? Load(AssemblyName assemblyName) => null;
}