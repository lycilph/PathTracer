namespace UI.Scripting;

/// <summary>
/// A structured compilation or runtime error from script execution.
/// </summary>
public sealed class ScriptError
{
    /// <summary>The error message.</summary>
    public string Message { get; }

    /// <summary>
    /// The line number in the script where the error occurred.
    /// 0 if the error has no location (e.g. runtime errors).
    /// </summary>
    public int Line { get; }

    /// <summary>
    /// The column number in the script where the error occurred.
    /// 0 if the error has no location.
    /// </summary>
    public int Column { get; }

    /// <summary>True if this error has a specific location in the script.</summary>
    public bool HasLocation => Line > 0;

    public ScriptError(string message, int line = 0, int column = 0)
    {
        Message = message;
        Line = line;
        Column = column;
    }

    public override string ToString() => HasLocation
        ? $"({Line},{Column}): {Message}"
        : Message;
}