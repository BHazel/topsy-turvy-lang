namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a variable assignment.
/// </summary>
/// <remarks>
/// <para>
/// This corresponds to the <c>IS APPOINTED</c> operator in Topsy Turvy.  Every variable assignment consists of the
/// name of the variable being assigned, the <see cref="Target"/>, and the value being assigned
/// represented by an <see cref="Expression"/>. The type of the value being assigned determines the type of the
/// <see cref="Expression"/>, which can be any <see cref="Node"/>.
/// </para>
/// <para>
/// For example, the following code in Topsy Turvy to assign a variable <c>LovesickMaidens</c> the literal Peer (integer)
/// value <c>20</c>:
/// </para>
/// <code>
/// LovesickMaidens IS APPOINTED 20
/// </code>
/// <para>
/// would be represented in the AST as an <see cref="AssignmentNode"/> with:
/// * The <see cref="Target"/> property set to <c>LovesickMaidens</c>.
/// * The <see cref="Value"/> property set to a <see cref="LiteralNode"/>, as <c>20</c> is a literal value, with:
///     * The <see cref="LiteralNode.Type"/> property set to <see cref="LiteralType.Integer"/>.
///     * The <see cref="LiteralNode.Value"/> property set to <c>20</c>.
/// </para>
/// <para>
/// When parsed this would be represented in the AST as:
/// </para>
/// <code>
/// new AssignmentNode()
/// {
///     Target = "LovesickMaidens",
///     Value = new LiteralNode()
///     {
///         Type = LiteralType.Integer,
///         Value = 20
///     },
///     Span = new() { /* ... */ }
/// };
/// </code>
/// <para>
/// For a more involved example, such as an assignment to the return of a function call with <c>SUMMON</c>:
/// </para>
/// <code>
/// LovesickMaidens IS APPOINTED SUMMON HowManyMaidens WITH NOTHING IF YOU PLEASE.
/// </code>
/// <para>
/// would be represented in the AST as an <see cref="AssignmentNode"/> with:
/// * The <see cref="Target"/> property set to <c>LovesickMaidens</c> as before.
/// * The <see cref="Value"/> property set to a <see cref="PrefixExpressionNode"/>, as a function call uses the <see cref="Operator"/><c>.Summon</c> operator, with:
///     * The <see cref="PrefixExpressionNode.Operator"/> property set to <see cref="Operator"/><c>.Summon</c>.
///     * The <see cref="PrefixExpressionNode.Arguments"/> property set to a list containing a single <see cref="IdentifierNode"/> with the name of the function being called.
/// </para>
/// <para>
/// and when parsed would be represented in the AST as:
/// </para>
/// <code>
/// new AssignmentNode()
/// {
///     Target = "LovesickMaidens",
///     Value = new PrefixExpressionNode()
///     {
///         Operator = Operator.Summon,
///         Arguments =
///         [
///             new IdentifierNode()
///             {
///                 Name = "HowManyMaidens"
///            }
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
public class AssignmentNode : Statement
{
    /// <summary>
    /// Gets or initialises the name of the variable to assign to.
    /// </summary>
    public required string Target { get; init; }

    /// <summary>
    /// Gets or initialises the value to assign.
    /// </summary>
    public required Expression Value { get; init; }
}
