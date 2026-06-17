namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a type cast that produces a value without mutating the original expression.
/// </summary>
/// <remarks>
/// <para>
/// This corresponds to the <c>AS IT WERE</c> ... <c>AS A</c> type casting statement in Topsy Turvy.  It allows converting a
/// value from one type to another without modifying the original variable.  Every statement cast consists of an
/// <see cref="Expression"/> to cast and a destination type, <see cref="NewType"/>.  The cast result is stored
/// in the implicit <c>JUST SO</c> variable and must be read from there in subsequent statements.
/// </para>
/// <para>
/// For example, the following code in Topsy Turvy casts a Peer (integer) literal to a Fathom (float) and then prints it:
/// </para>
/// <code>
/// AS IT WERE 20 AS A FATHOM
/// BEHOLD JUST SO
/// </code>
/// <para>
/// would be represented in the AST as two consecutive statements: an <see cref="ExpressionCastNode"/> followed by a
/// <see cref="PrintNode"/> that reads the <c>JUST SO</c> implicit variable:
/// </para>
/// <para>
/// * The <see cref="ExpressionCastNode"/> with:
///     * The <see cref="Expression"/> property set to a <see cref="LiteralNode"/>, as <c>20</c> is a literal, with:
///         * The <see cref="LiteralNode.Type"/> property set to <see cref="LiteralType.Integer"/>.
///         * The <see cref="LiteralNode.Value"/> property set to <c>20</c>.
///     * The <see cref="NewType"/> property set to <see cref="LiteralType"/>.<c>Float</c>.
/// * The <see cref="PrintNode"/> with its <see cref="PrintNode.Expression"/> set to an <see cref="IdentifierNode"/> with
///   <see cref="IdentifierNode.Name"/> set to <c>JUST SO</c> (or convenience constant <see cref="Keywords"/><c>.SpecialNames.JustSo</c>).
/// </para>
/// <para>
/// When parsed this would be represented in the AST as:
/// </para>
/// <code>
/// new ExpressionCastNode()
/// {
///     Expression = new LiteralNode()
///     {
///         Type = LiteralType.Integer,
///         Value = 20,
///         Span = new() { /* ... */ }
///     },
///     NewType = LiteralType.Float,
///     Span = new() { /* ... */ }
/// };
/// 
/// new PrintNode()
/// {
///     Expression = new IdentifierNode()
///     {
///         Name = Keywords.SpecialNames.JustSo,    // "JUST SO"
///         Span = new() { /* ... */ }
///     },
///     Span = new() { /* ... */ }
/// };
/// </code>
/// <para>
/// For a more involved example, casting a variable before using its value in a declaration:
/// </para>
/// <code>
/// AS IT WERE ParadoxBirthday AS A FATHOM
/// PRAY WELCOME Years AS A FATHOM BEING QUOTIENT OF JUST SO AND 4
/// </code>
/// <para>
/// would be represented in the AST as two consecutive statements: an <see cref="ExpressionCastNode"/> followed by a
/// <see cref="DeclarationNode"/>:
/// </para>
/// <para>
/// * The <see cref="ExpressionCastNode"/> with:
///     * The <see cref="Expression"/> property set to an <see cref="IdentifierNode"/>, as <c>ParadoxBirthday</c> is a variable, with:
///         * The <see cref="IdentifierNode.Name"/> property set to <c>ParadoxBirthday</c>.
///     * The <see cref="NewType"/> property set to <see cref="LiteralType"/>.<c>Float</c>.
/// * The <see cref="DeclarationNode"/> with:
///     * The <see cref="DeclarationNode.Name"/> property set to <c>Years</c>.
///     * The <see cref="DeclarationNode.Type"/> property set to <see cref="LiteralType"/>.<c>Float</c>.
///     * The <see cref="DeclarationNode.InitialValue"/> property set to a <see cref="PrefixExpressionNode"/> using the <see cref="Operator"/>.<c>Quotient</c> operator, with:
///         * <see cref="PrefixExpressionNode.Arguments"/> containing an <see cref="IdentifierNode"/> for <c>JUST SO</c> and a <see cref="LiteralNode"/> for <c>4</c>.
/// </para>
/// <para>
/// and when parsed would be represented in the AST as:
/// </para>
/// <code>
/// new ExpressionCastNode()
/// {
///     Expression = new IdentifierNode()
///     {
///         Name = "ParadoxBirthday",
///         Span = new() { /* ... */ }
///     },
///     NewType = LiteralType.Float,
///     Span = new() { /* ... */ }
/// };
/// 
/// new DeclarationNode()
/// {
///     Name = "Years",
///     Type = LiteralType.Float,
///     InitialValue = new PrefixExpressionNode()
///     {
///         Operator = Operator.Quotient,
///         Arguments =
///         [
///             new IdentifierNode()
///             {
///                 Name = Keywords.SpecialNames.JustSo,    // "JUST SO"
///                 Span = new() { /* ... */ }
///             },
///             new LiteralNode()
///             {
///                 Type = LiteralType.Integer,
///                 Value = 4,
///                 Span = new() { /* ... */ }
///             }
///         ],
///         Span = new() { /* ... */ }
///     },
///     Span = new() { /* ... */ }
/// };
/// </code>
/// <para>
/// Please note examples have commented out sections for brevity.
/// </para>
/// </remarks>
public class ExpressionCastNode : TypeCastNode
{
    /// <summary>
    /// Gets or initialises the expression to cast.
    /// </summary>
    public required Expression Expression { get; init; }

    /// <summary>
    /// Gets or initialises the destination type.
    /// </summary>
    public required LiteralType NewType { get; init; }
}
