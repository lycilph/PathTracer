namespace Scripting;

public sealed record CompilationResult(
    bool Success,
    string? ErrorText,
    IReadOnlyList<ScriptDiagnostic> Diagnostics);