namespace ScriptApi.Validation;

/// <summary>
/// Severity level of a validation message.
/// </summary>
public enum ValidationSeverity
{
    /// <summary>
    /// A potential issue that will not prevent rendering but may produce
    /// unexpected results.
    /// </summary>
    Warning,

    /// <summary>
    /// A critical issue that will prevent the scene from rendering correctly.
    /// </summary>
    Error
}

/// <summary>
/// A single validation message produced during scene construction.
/// </summary>
public sealed class ValidationMessage
{
    /// <summary>The severity of this message.</summary>
    public ValidationSeverity Severity { get; }

    /// <summary>A human-readable description of the issue.</summary>
    public string Message { get; }

    /// <summary>
    /// The name of the primitive this message relates to, or null if
    /// the message applies to the scene as a whole.
    /// </summary>
    public string? PrimitiveName { get; }

    public ValidationMessage(ValidationSeverity severity, string message,
                             string? primitiveName = null)
    {
        Severity = severity;
        Message = message;
        PrimitiveName = primitiveName;
    }

    public override string ToString() =>
        PrimitiveName is null
            ? $"[{Severity}] {Message}"
            : $"[{Severity}] {PrimitiveName}: {Message}";
}