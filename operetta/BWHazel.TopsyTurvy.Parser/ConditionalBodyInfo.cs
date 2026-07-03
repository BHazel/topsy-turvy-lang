using System.Collections.Generic;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Parser;

/// <summary>
/// Information on the body of a conditional statement.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="ConditionalBodyInfo"/> is an intermediate parse result that captures the branch structure of a
/// conditional statement: the block between the opening condition and the closing <c>SO MUCH FOR THAT.</c>
/// keyword.  It is produced by the <see cref="StatementParser"/><c>.ConditionalBody</c> parser and its fields are
/// then used to populate the corresponding <see cref="ConditionalNode"/> properties.  It is not itself a
/// <see cref="Node"/> and does not appear in the finished AST.
/// </para>
/// <para>
/// The three properties map directly onto <see cref="ConditionalNode"/>.
/// </para>
/// </remarks>
/// <param name="TrueBlock">The block of statements to execute if the condition is true.</param>
/// <param name="ElseIfs">The list of else-if branches, empty if no <c>OR, IF NOT,</c> clauses are present.</param>
/// <param name="ElseBlock">The block to execute if all conditions were false, empty when no <c>OTHERWISE,</c> clause is present.</param>
public record ConditionalBodyInfo(
    IReadOnlyList<Statement> TrueBlock,
    IReadOnlyList<ElseIfBranch> ElseIfs,
    IReadOnlyList<Statement> ElseBlock);
