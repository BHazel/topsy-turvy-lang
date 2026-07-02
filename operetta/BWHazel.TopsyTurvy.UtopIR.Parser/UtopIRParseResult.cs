using System.Collections.Generic;
using BWHazel.TopsyTurvy.UtopIR.Ast;

namespace BWHazel.TopsyTurvy.UtopIR.Parser;

/// <summary>
/// The result of attempting to parse UtopIR source text.
/// </summary>
/// <param name="Program">The parsed programme, or <c>null</c> if parsing failed.</param>
/// <param name="Diagnostics">The diagnostics produced while parsing, empty on success.</param>
public sealed record UtopIRParseResult(UtopIRProgram? Program, IReadOnlyList<UtopIRDiagnostic> Diagnostics)
{
    /// <summary>
    /// Gets a value indicating whether parsing succeeded.
    /// </summary>
    public bool Success => this.Program is not null;
}
