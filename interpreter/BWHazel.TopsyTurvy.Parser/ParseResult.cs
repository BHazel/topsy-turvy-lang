using System.Collections.Generic;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Parser;

/// <summary>
/// Represents the outcome of a parse attempt.
/// </summary>
/// <remarks>
/// <para>
/// This holds the result of a parse of a Topsy Turvy programme using the <see cref="TopsyTurvyParser.TryParse"/> method, containing:
/// * The <see cref="ProgramNode"/> on a successful parse, or <c>null</c> if it failed.
/// * A collection of <see cref="Diagnostic"/> objects describing any issues encountered during parsing.
///     * It should be noted that there may be diagnostics present even on a successful parse.
/// </para>
/// <code>
/// TopsyTurvyParser parser = new();
/// string sourceCode = File.ReadAllText("programme.topsy");
/// ParseResult result = parser.TryParse(sourceCode);
/// if (result.Success)
/// {
///     ProgramNode program = result.Program!;
///     // Process the program...
/// }
/// else
/// {
///     Console.WriteLine("Syntax error(s) found:");
///     foreach (Diagnostic diagnostic in result.Diagnostics)
///     {
///         Console.WriteLine(diagnostic.Message);
///     }
/// }
/// </code>
/// </remarks>
/// <param name="Program">The parsed program node, or <c>null</c> if parsing failed.</param>
/// <param name="Diagnostics">The collection of diagnostics produced during parsing.</param>
public record ParseResult(ProgramNode? Program, IReadOnlyList<Diagnostic> Diagnostics)
{
    /// <summary>
    /// Gets a value indicating whether parsing succeeded.
    /// </summary>
    public bool Success => this.Program is not null;
}
