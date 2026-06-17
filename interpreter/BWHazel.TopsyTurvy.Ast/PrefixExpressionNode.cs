using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a prefix-notation operator expression in the AST.
/// </summary>
/// <remarks>
/// <para>
/// This is the primary expression type in Topsy Turvy.  All <see cref="Operator"/>s are expressed in prefix notation
///  (<c>OPERATOR operand1 AND operand2</c>) and produce a <see cref="PrefixExpressionNode"/>.  Every node holds the
/// <see cref="Operator"/> being applied and an <see cref="Arguments"/> list of operands as <see cref="Expression"/>s:
/// * For binary operators it contains exactly two entries.
/// * For unary operators (currently only <c>HARDLY EVER</c>) it contains exactly one entry.
/// * For variadic operators (<c>WOVEN OF</c>, <c>ALL OF</c>, <c>ANY OF</c> and <c>SUMMON</c>) the argument list is open-ended, closed by <c>IF YOU PLEASE.</c>.    
/// Please see <see cref="Operator"/> for the full list of supported operators.
/// </para>
/// <para>
/// For example, the following code in Topsy Turvy to add two variables:
/// </para>
/// <code>
/// SUM OF ConservativePeers AND LiberalPeers
/// </code>
/// <para>
/// would be represented in the AST as a <see cref="PrefixExpressionNode"/> with:
/// * The <see cref="Operator"/> property set to <see cref="Operator"/>.<c>Sum</c>.
/// * The <see cref="Arguments"/> property set to a list containing:
///     * An <see cref="IdentifierNode"/> with <see cref="IdentifierNode.Name"/> set to <c>ConservativePeers</c>.
///     * An <see cref="IdentifierNode"/> with <see cref="IdentifierNode.Name"/> set to <c>LiberalPeers</c>.
/// </para>
/// <para>
/// When parsed this would be represented in the AST as:
/// </para>
/// <code>
/// new PrefixExpressionNode()
/// {
///     Operator = Operator.Sum,
///     Arguments =
///     [
///         new IdentifierNode()
///         {
///             Name = "ConservativePeers",
///             Span = new() { /* ... */ }
///         },
///         new IdentifierNode()
///         {
///             Name = "LiberalPeers",
///             Span = new() { /* ... */ }
///         }
///     ],
///     Span = new() { /* ... */ }
/// };
/// </code>
/// <para>
/// For a variadic example, concatenating three values with <c>WOVEN OF</c>:
/// </para>
/// <code>
/// WOVEN OF "Lord " AND Title AND " of Titipu" IF YOU PLEASE.
/// </code>
/// <para>
/// would be represented in the AST as a <see cref="PrefixExpressionNode"/> with:
/// * The <see cref="Operator"/> property set to <see cref="Ast.Operator"/>.<c>WovenOf</c>.
/// * The <see cref="Arguments"/> property set to a list containing a <see cref="LiteralNode"/> for <c>Lord </c>,
///   an <see cref="IdentifierNode"/> for <c>Title</c>, and a <see cref="LiteralNode"/> for <c> of Titipu</c>.
/// </para>
/// <para>
/// Please note examples have commented out sections for brevity.
/// </para>
/// </remarks>
public class PrefixExpressionNode : Expression
{
    /// <summary>
    /// Gets or initialises the operator being applied.
    /// </summary>
    public required Operator Operator { get; init; }

    /// <summary>
    /// Gets or initialises the arguments (operands) of the expression.
    /// </summary>
    public required IReadOnlyList<Expression> Arguments { get; init; }
}
