using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a loop statement.
/// </summary>
/// <remarks>
/// <para>
/// This corresponds to the <c>BY A LEGAL FICTION</c> ... <c>THE TERM EXPIRES.</c> loop block in Topsy Turvy.  Every loop
/// consists of an optional <see cref="Label"/>, a <see cref="Type"/> determining the loop form, an optional
/// <see cref="Condition"/> expression, an optional variable, <see cref="LoopVariable"/>, for counted loops and a <see cref="Body"/>
/// of <see cref="Statement"/>s.  Which of these are present depends on the <see cref="LoopType"/>.
/// </para>
/// <para>
/// For example, the following ascending loop in Topsy Turvy counts from 0 to 7:
/// </para>
/// <code>
/// BY A LEGAL FICTION KNOWN AS HeavyDragoons ASCENDING count UNTIL ALIKE count AND 7
///     BEHOLD "A heavy dragoon!"
/// THE TERM EXPIRES.
/// </code>
/// <para>
/// would be represented in the AST as a <see cref="LoopNode"/> with:
/// * The <see cref="Label"/> property set to <c>HeavyDragoons</c>.
/// * The <see cref="Type"/> property set to <see cref="LoopType"/>.<c>Ascending</c>.
/// * The <see cref="Condition"/> property set to a <see cref="PrefixExpressionNode"/> using the <see cref="Operator"/>.<c>Alike</c> operator, with:
///     * <see cref="PrefixExpressionNode.Arguments"/> containing an <see cref="IdentifierNode"/> for <c>count</c> and a <see cref="LiteralNode"/> for <c>7</c>.
/// * The <see cref="LoopVariable"/> property set to <c>count</c>.
/// * The <see cref="Body"/> property set to a list containing a single <see cref="PrintNode"/> for the <c>BEHOLD</c> statement.
/// </para>
/// <para>
/// When parsed this would be represented in the AST as:
/// </para>
/// <code>
/// new LoopNode()
/// {
///     Label = "HeavyDragoons",
///     Type = LoopType.Ascending,
///     Condition = new PrefixExpressionNode()
///     {
///         Operator = Operator.Alike,
///         Arguments =
///         [
///             new IdentifierNode()
///             {
///                 Name = "count",
///                 Span = new() { /* ... */ }
///             },
///             new LiteralNode()
///             {
///                 Type = LiteralType.Integer,
///                 Value = 7,
///                 Span = new() { /* ... */ }
///             }
///         ],
///         Span = new() { /* ... */ }
///     },
///     LoopVariable = "count",
///     Body =
///     [
///         new PrintNode() { /* ... */ }
///     ],
///     Span = new() { /* ... */ }
/// };
/// </code>
/// <para>
/// For a while (<c>WHILST</c>) loop, <see cref="LoopVariable"/> is <c>null</c> and the <see cref="Condition"/> is a
/// boolean expression.
/// </para>
/// <para>
/// For an infinite loop, <see cref="Condition"/> and <see cref="LoopVariable"/> are both <c>null</c> and the
/// <see cref="Body"/> must contain a <see cref="BreakNode"/> (<c>THAT WILL DO.</c>) to exit.
/// </para>
/// <para>
/// Please note examples have commented out sections for brevity.
/// </para>
/// </remarks>
public class LoopNode : Statement
{
    /// <summary>
    /// Gets or initialises the optional label for the loop.
    /// </summary>
    public string? Label { get; init; }

    /// <summary>
    /// Gets or initialises the type of the loop.
    /// </summary>
    public required LoopType Type { get; init; }

    /// <summary>
    /// Gets or initialises the exit or continuation condition.
    /// </summary>
    public Expression? Condition { get; init; }

    /// <summary>
    /// Gets or initialises the variable used for counting in ascending/descending loops.
    /// </summary>
    public string? LoopVariable { get; init; }

    /// <summary>
    /// Gets or initialises the body of the loop.
    /// </summary>
    public required IReadOnlyList<Statement> Body { get; init; }
}
