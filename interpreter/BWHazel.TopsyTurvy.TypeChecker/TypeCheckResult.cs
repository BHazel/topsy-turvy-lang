using System.Collections.Generic;
using System.Linq;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.TypeChecker;

/// <summary>
/// Represents the outcome of a type-check pass over a Topsy Turvy programme.
/// </summary>
/// <remarks>
/// A successful result means no <see cref="DiagnosticSeverity.Error"/> diagnostics were produced but
/// warnings may still be present.  When unsuccessful, the programme contains at least one type error
/// and should not be executed.
/// </remarks>
/// <param name="Model">The semantic model produced by the type-check pass.</param>
/// <param name="Diagnostics">All diagnostics emitted during the type-check pass.</param>
public record TypeCheckResult(
    SemanticModel Model,
    IReadOnlyList<Diagnostic> Diagnostics)
{
    /// <summary>
    /// Gets a value indicating whether the type check passed with no errors.
    /// </summary>
    /// <remarks>Warnings do not prevent execution, only <see cref="DiagnosticSeverity.Error"/> diagnostics do.</remarks>
    public bool Success =>
        !this.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
}
