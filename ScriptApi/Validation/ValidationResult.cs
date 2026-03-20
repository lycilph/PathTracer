namespace ScriptApi.Validation;

/// <summary>
/// Collects all validation messages produced during scene construction.
/// Contains both warnings and errors.
/// </summary>
public sealed class ValidationResult
{
    private readonly List<ValidationMessage> _messages = [];

    /// <summary>All validation messages in the order they were added.</summary>
    public IReadOnlyList<ValidationMessage> Messages => _messages;

    /// <summary>All error-level messages.</summary>
    public IEnumerable<ValidationMessage> Errors =>
        _messages.Where(m => m.Severity == ValidationSeverity.Error);

    /// <summary>All warning-level messages.</summary>
    public IEnumerable<ValidationMessage> Warnings =>
        _messages.Where(m => m.Severity == ValidationSeverity.Warning);

    /// <summary>True if there are no error-level messages.</summary>
    public bool IsValid => !_messages.Any(m => m.Severity == ValidationSeverity.Error);

    /// <summary>True if there are any warning-level messages.</summary>
    public bool HasWarnings => _messages.Any(m => m.Severity == ValidationSeverity.Warning);

    /// <summary>Adds an error message.</summary>
    public void AddError(string message, string? primitiveName = null) =>
        _messages.Add(new ValidationMessage(ValidationSeverity.Error,
                                            message, primitiveName));

    /// <summary>Adds a warning message.</summary>
    public void AddWarning(string message, string? primitiveName = null) =>
        _messages.Add(new ValidationMessage(ValidationSeverity.Warning,
                                            message, primitiveName));

    /// <summary>
    /// Returns a formatted summary of all messages, grouped by severity.
    /// </summary>
    public string Summary()
    {
        if (!_messages.Any())
            return "Validation passed with no issues.";

        var sb = new System.Text.StringBuilder();

        var errors = Errors.ToList();
        var warnings = Warnings.ToList();

        if (errors.Count > 0)
        {
            sb.AppendLine($"{errors.Count} error(s):");
            foreach (var e in errors)
                sb.AppendLine($"  {e}");
        }

        if (warnings.Count > 0)
        {
            sb.AppendLine($"{warnings.Count} warning(s):");
            foreach (var w in warnings)
                sb.AppendLine($"  {w}");
        }

        return sb.ToString().TrimEnd();
    }
}