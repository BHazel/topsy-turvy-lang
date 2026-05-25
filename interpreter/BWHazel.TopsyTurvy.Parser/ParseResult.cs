using System.Collections.Generic;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Parser;

/// <summary>
/// Represents the outcome of a parse attempt.
/// </summary>
/// <param name="Program">The parsed program node, or <c>null</c> if parsing failed.</param>
/// <param name="Diagnostics">The collection of diagnostics produced during parsing.</param>
public record ParseResult(ProgramNode? Program, IReadOnlyList<Diagnostic> Diagnostics)
{
    /// <summary>
    /// Gets a value indicating whether parsing succeeded.
    /// </summary>
    public bool Success => this.Program is not null;
}
