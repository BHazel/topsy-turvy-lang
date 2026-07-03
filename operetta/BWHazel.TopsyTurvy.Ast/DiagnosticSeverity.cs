namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Specifies the severity level of a diagnostic.
/// </summary>
/// <remarks>
/// <para>
/// Every <see cref="Diagnostic"/> is classified by one of three severity levels:
/// </para>
/// <para>
/// * <see cref="DiagnosticSeverity"/>.<c>Info</c>: An informational message that does not indicate a problem.
/// * <see cref="DiagnosticSeverity"/>.<c>Warning</c>: A potential issue that does not prevent execution but should be reviewed.
/// * <see cref="DiagnosticSeverity"/>.<c>Error</c>: A critical error that prevents successful parsing or execution.  The presence of at least one <c>Error</c> diagnostic is indicated by <see cref="DiagnosticCollection.HasErrors"/>.
/// </para>
/// </remarks>
public enum DiagnosticSeverity
{
    /// <summary>Informational message.</summary>
    Info,

    /// <summary>Potential issue that should be reviewed.</summary>
    Warning,

    /// <summary>Critical error that prevents successful execution or parsing.</summary>
    Error
}
