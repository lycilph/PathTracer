namespace Studio;


public sealed class ScriptDiagnosticItem
{
    public string Id { get; init; } = "";
    public string Severity { get; init; } = "";
    public int Line { get; init; }      // 1-based
    public int Column { get; init; }    // 1-based
    public string Message { get; init; } = "";

    // Exact squiggle span (0-based document offset + length)
    public int StartOffset { get; init; }
    public int Length { get; init; }

    public string Display => $"{Severity} {Id} ({Line},{Column}): {Message}";
}
