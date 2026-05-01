using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Core.Scene;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace Scripting;

public sealed class RoslynSceneScriptEngine : ISceneScriptEngine
{
    private readonly ScriptOptions _options;

    // Cache compiled delegates keyed by hash of script text
    private readonly ConcurrentDictionary<string, ScriptRunner<SceneDefinition>> _cache = new();

    public RoslynSceneScriptEngine()
    {
        // Minimal set of assemblies:
        // - core BCL
        // - Tracer.Core (for Scene/Camera/Math/etc.)
        // - Tracer.Scripting (for SceneApi/SceneDefinition)
        //
        // Roslyn scripting supports configuring references/imports through ScriptOptions. [1](https://github.com/dotnet/roslyn/blob/main/docs/wiki/Scripting-API-Samples.md)[2](https://www.nuget.org/packages/Microsoft.CodeAnalysis.CSharp.Scripting/)
        var tracerCoreAsm = typeof(Scene).Assembly;
        var tracerScriptingAsm = typeof(SceneDefinition).Assembly;

        _options = ScriptOptions.Default
            .WithReferences(
                typeof(object).Assembly,          // System.Private.CoreLib
                typeof(Enumerable).Assembly,      // System.Linq
                tracerCoreAsm,
                tracerScriptingAsm)
            .WithImports(
                "System",
                "System.Linq",
                "Scripting",
                "Core.Math",
                "Core.Scene",
                "Core.Camera",
                "Core.Materials",
                "Core.Lights");
    }


    public CompilationResult TryCompile(string code)
    {
        try
        {
            var script = CSharpScript.Create<SceneDefinition>(
                code,
                _options,
                globalsType: typeof(ScriptGlobals));

            var diagnostics = script.Compile();

            var diags = diagnostics
                .Where(d => d.Severity >= DiagnosticSeverity.Info)
                .Select(ToScriptDiagnostic)
                .Where(d => d is not null)
                .Select(d => d!)
                .ToList();

            bool success = !diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error);

            return new CompilationResult(
                Success: success,
                ErrorText: success ? null : "Script has compilation errors.",
                Diagnostics: diags);
        }
        catch (Exception ex)
        {
            return new CompilationResult(false, ex.ToString(), Array.Empty<ScriptDiagnostic>());
        }
    }

    private static ScriptDiagnostic? ToScriptDiagnostic(Diagnostic d)
    {
        // Some diagnostics might not be in source (e.g., metadata/reference issues).
        // For squiggles we only want in-source spans.
        if (!d.Location.IsInSource)
            return null;

        var span = d.Location.SourceSpan;           // start+length in the script text
        var lineSpan = d.Location.GetLineSpan();    // line/col info

        int line = lineSpan.StartLinePosition.Line + 1;
        int col = lineSpan.StartLinePosition.Character + 1;

        return new ScriptDiagnostic(
            Id: d.Id,
            Severity: d.Severity,
            Message: d.GetMessage(),
            SpanStart: span.Start,
            SpanLength: span.Length,
            Line: line,
            Column: col);
    }

    public async Task<SceneDefinition> ExecuteAsync(string code, int width, int height, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        string key = Hash(code);

        // Get or compile delegate
        var runner = _cache.GetOrAdd(key, _ =>
        {
            var script = CSharpScript.Create<SceneDefinition>(
                code,
                _options,
                globalsType: typeof(ScriptGlobals));

            var diagnostics = script.Compile();
            if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
            {
                string err = string.Join(Environment.NewLine, diagnostics.Select(FormatDiagnostic));
                throw new CompilationErrorException(err, diagnostics);
            }

            return script.CreateDelegate();
        });

        var globals = new ScriptGlobals(width, height);

        // Execute (supports cancellation token)
        // ScriptRunner supports a CancellationToken overload. [1](https://github.com/dotnet/roslyn/blob/main/docs/wiki/Scripting-API-Samples.md)[2](https://www.nuget.org/packages/Microsoft.CodeAnalysis.CSharp.Scripting/)
        var result = await runner(globals, token);
        if (result is null)
            throw new InvalidOperationException("Script returned null SceneDefinition.");

        return result;
    }

    private static string Hash(string text)
    {
        // Stable hash for caching
        using var sha = SHA256.Create();
        byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes);
    }

    private static string FormatDiagnostic(Diagnostic d)
    {
        var span = d.Location.GetLineSpan();
        int line = span.StartLinePosition.Line + 1;
        int col = span.StartLinePosition.Character + 1;
        return $"{d.Severity} {d.Id} ({line},{col}): {d.GetMessage()}";
    }
}