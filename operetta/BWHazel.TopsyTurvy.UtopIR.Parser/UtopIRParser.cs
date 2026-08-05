using System.Linq;
using BWHazel.TopsyTurvy.UtopIR.Ast;
using Superpower;
using Superpower.Model;

namespace BWHazel.TopsyTurvy.UtopIR.Parser;

/// <summary>
/// Public entry point for parsing UtopIR source text into a <see cref="UtopIRProgram"/>.
/// </summary>
/// <remarks>
/// <para>
/// This is the top and main entry point for the UtopIR parser, which builds on top of the lexer,
/// operand and instruction parsers to build a single <see cref="UtopIRProgram"/> AST component.  As
/// there is no pre-processor pipeline or Source Map, the AST carries no source span information;
/// diagnostics are instead reported directly from the parsing process via (see <see cref="TryParse"/>).
/// </para>
/// <para>
/// The whole programme is parsed by <see cref="InstructionParser.FunctionDefinitionSequence"/>, which
/// matches one or more <c>duty</c> ... <c>discharged</c> function definition blocks to end-of-input.
/// </para>
/// <para>
/// ### Example
/// Given the UtopIR source text:
/// <code>
/// duty &amp;Opera, finds peer
///     £result = welcome peer
///     £result = appoint 13
///     find £result
/// discharged
/// </code>
/// both <see cref="Parse"/> and <see cref="TryParse"/> return a <see cref="UtopIRProgram"/> whose
/// <see cref="UtopIRProgram.Functions"/> is a single-element list: a <see cref="UtopIRFunctionDefinition"/>
/// named <c>Opera</c>, returning <see cref="UtopIRType.Peer"/>, whose <see cref="UtopIRFunctionDefinition.Body"/>
/// is a three-element list:
/// * A <see cref="WelcomeInstruction"/> declaring <c>result</c> as <see cref="UtopIRType.Peer"/>.
/// * A <see cref="AppointInstruction"/> assigning the literal <c>13</c>.
/// * A <see cref="FindInstruction"/> returning the <c>result</c> value.
/// This is the same shape <c>TopsyTurvyToUtopIRTransformer</c> would produce from equivalent Topsy Turvy source and what
/// <c>CilEmitter</c> consumes to produce a runnable assembly.
/// </para>
/// </remarks>
public sealed class UtopIRParser
{
    /// <summary>
    /// Parses UtopIR source text into a <see cref="UtopIRProgram"/>, throwing on failure.
    /// </summary>
    /// <param name="source">The UtopIR source text to parse.</param>
    /// <returns>The parsed programme.</returns>
    /// <exception cref="UtopIRSyntaxException">Thrown when <paramref name="source"/> fails to parse.</exception>
    public UtopIRProgram Parse(string source)
    {
        UtopIRParseResult result = this.TryParse(source);
        if (!result.Success)
        {
            throw new UtopIRSyntaxException(result.Diagnostics.Select(diagnostic => diagnostic.Message));
        }

        return result.Program!;
    }

    /// <summary>
    /// Attempts to parse UtopIR source text into a <see cref="UtopIRProgram"/>.
    /// </summary>
    /// <param name="source">The UtopIR source text to parse.</param>
    /// <returns>A <see cref="UtopIRParseResult"/> describing the outcome.</returns>
    public UtopIRParseResult TryParse(string source)
    {
        Result<UtopIRFunctionDefinition[]> parseResult = InstructionParser.FunctionDefinitionSequence().TryParse(source);
        if (!parseResult.HasValue)
        {
            string message = !string.IsNullOrEmpty(parseResult.ErrorMessage)
                ? parseResult.ErrorMessage
                : parseResult.Expectations is { Length: > 0 }
                    ? $"Expected: {string.Join(", ", parseResult.Expectations)}"
                    : "Syntax error";

            UtopIRDiagnostic diagnostic = new(message, parseResult.ErrorPosition.Line, parseResult.ErrorPosition.Column);
            return new(null, [diagnostic]);
        }

        return new(new UtopIRProgram(parseResult.Value), []);
    }
}
