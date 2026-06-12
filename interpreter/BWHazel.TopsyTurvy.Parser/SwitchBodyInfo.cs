using System.Collections.Generic;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Parser;

/// <summary>
/// Carries the parsed cases and default block of a switch statement body.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SwitchBodyInfo"/> is an intermediate parse result that captures the case structure of a switch
/// statement: the block between the opening expression and the closing
/// <c>NOTHING COULD BE MORE SATISFACTORY.</c> keyword.  It is produced by the
/// <see cref="StatementParser"/><c>.SwitchBody</c> parser and its fields are then used to populate the corresponding
/// <see cref="SwitchNode"/> properties.  It is not itself a <see cref="Node"/> and does not appear in the
/// finished AST.
/// </para>
/// <para>
/// The two fields map directly onto <see cref="SwitchNode"/>.
/// </para>
/// </remarks>
/// <param name="Cases">Zero or more cases, each introduced by <c>WHEN ACTING AS</c></param>
/// <param name="DefaultBlock">The statements in the optional <c>FAILING ALL OF THE ABOVE,</c> default block, or an empty list when absent.</param>
public record SwitchBodyInfo(
    IReadOnlyList<SwitchCase> Cases,
    IReadOnlyList<Statement> DefaultBlock);
