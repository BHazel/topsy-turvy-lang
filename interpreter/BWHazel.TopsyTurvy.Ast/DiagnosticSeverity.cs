namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Specifies the severity level of a diagnostic.
/// </summary>
public enum DiagnosticSeverity
{
    /// <summary>Informational message.</summary>
    Info,

    /// <summary>Potential issue that should be reviewed.</summary>
    Warning,

    /// <summary>Critical error that prevents successful execution or parsing.</summary>
    Error
}
