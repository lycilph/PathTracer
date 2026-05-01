using Microsoft.CodeAnalysis;

namespace Scripting;

public sealed record ScriptDiagnostic(
    string Id,
    DiagnosticSeverity Severity,
    string Message,
    int SpanStart,     // 0-based character offset into the script text
    int SpanLength,    // length in characters
    int Line,          // 1-based
    int Column         // 1-based
);