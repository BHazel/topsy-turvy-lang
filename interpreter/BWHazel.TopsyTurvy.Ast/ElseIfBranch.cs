using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a single else-if branch.
/// </summary>
/// <remarks>
/// <para>
/// This corresponds to the <c>OR, IF NOT,</c> clause within a conditional block in Topsy Turvy.  Every else-if branch consists
/// of a <see cref="Condition"/> to evaluate as an <see cref="Expression"/> and a <see cref="Block"/> of <see cref="Statement"/>s
/// to execute if the condition evaluates to true.
/// </para>
/// <para>
/// <c>OR, IF NOT,</c> else-if branches are not standalone statements but are always part of a <c>SHOULD IT TRANSPIRE THAT</c>
/// <see cref="ConditionalNode"/>, forming a chain of conditions evaluated in sequence until one is true.  If none evaluate to
/// true, the execution falls through to the optional <see cref="ConditionalNode.ElseBlock"/> if present.
/// </para>
/// <para>
/// For example, within the conditional block checking a character name:
/// </para>
/// <code>
/// OR, IF NOT, ALIKE CharacterName AND "Bunthorne"
///     BEHOLD "Oh Hollow, Hollow, Hollow!"
/// </code>
/// <para>
/// this <c>OR, IF NOT,</c> clause would be represented in the AST as an <see cref="ElseIfBranch"/> with:
/// * The <see cref="Condition"/> property set to a <see cref="PrefixExpressionNode"/> using the <see cref="Operator"/><c>.Alike</c> operator, with:
///     * <see cref="PrefixExpressionNode.Arguments"/> containing an <see cref="IdentifierNode"/> for <c>CharacterName</c> and a <see cref="LiteralNode"/> for <c>Bunthorne</c>.
/// * The <see cref="Block"/> property set to a list containing a single <see cref="PrintNode"/> for the <c>BEHOLD</c> statement.
/// </para>
/// <para>
/// and when parsed would be represented in the AST as:
/// </para>
/// <code>
/// new ElseIfBranch(
///     Condition: new PrefixExpressionNode()
///     {
///         Operator = Operator.Alike,
///         Arguments =
///         [
///             new IdentifierNode()
///             {
///                 Name = "CharacterName",
///                 Span = new() { /* ... */ }
///             },
///             new LiteralNode()
///             {
///                 Value = "Bunthorne",
///                 Type = LiteralType.String,
///                 Span = new() { /* ... */ }
///             }
///         ],
///         Span = new() { /* ... */ }
///     },
///     Block:
///     [
///         new PrintNode()
///         {
///             Expression = new LiteralNode() { /* ... */ },
///             Span = new() { /* ... */ }
///         }
///     ]
/// );
/// </code>
/// <para>
/// Please note examples have commented out sections for brevity.
/// </para>
/// </remarks>
/// <param name="Condition">The condition to evaluate for this branch.</param>
/// <param name="Block">The block of statements to execute if the condition is true.</param>
public record ElseIfBranch(Expression? Condition, IReadOnlyList<Statement> Block);
