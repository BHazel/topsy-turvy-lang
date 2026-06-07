namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a variable declaration.
/// </summary>
/// <remarks>
/// <para>
/// This corresponds to the <c>PRAY WELCOME</c> ... <c>BEING</c> variable declaration statement in Topsy Turvy.  Every declaration consists
/// of the <see cref="Name"/> of the variable being declared, the <see cref="Type"/> of the variable and an optional initial
/// value represented by an <see cref="Expression"/>. The type of the initial value determines the type of the <see cref="Expression"/>,
/// which can be any <see cref="Node"/>.  If no initial value is provided, the <see cref="InitialValue"/> property will be <c>null</c>.
/// </para>
/// <para>
/// For example, the following code in Topsy Turvy to declare and assign a variable <c>LovesickMaidens</c> the literal Peer (integer)
/// value <c>20</c>:
/// </para>
/// <code>
/// PRAY WELCOME LovesickMaidens AS A PEER BEING 20
/// </code>
/// <para>
/// would be represented in the AST as a <see cref="DeclarationNode"/> with:
/// * The <see cref="Name"/> property set to <c>LovesickMaidens</c>.
/// * The <see cref="Type"/> property set to <see cref="LiteralType"/><c>.Integer</c>.
/// * The <see cref="InitialValue"/> property set to a <see cref="LiteralNode"/>, as <c>20</c> is a literal value, with:
///     * The <see cref="LiteralNode.Type"/> property set to <see cref="LiteralType"/><c>.Integer</c>.
///     * The <see cref="LiteralNode.Value"/> property set to <c>20</c>.
/// </para>
/// <para>
/// When parsed this would be represented in the AST as:
/// </para>
/// <code>
/// new DeclarationNode()
/// {
///     Name = "LovesickMaidens",
///     Type = LiteralType.Integer,
///     InitialValue = new LiteralNode()
///     {
///         Type = LiteralType.Integer,
///         Value = 20
///     },
///     Span = new() { /* ... */ }
/// };
/// </code>
/// <para>
/// For a more involved example, such as a declaration of a variable with an initial value set to the return of a
/// function call with <c>SUMMON</c>:
/// </para>
/// <code>
/// PRAY WELCOME LovesickMaidens AS A PEER BEING SUMMON HowManyMaidens WITH NOTHING IF YOU PLEASE.
/// </code>
/// <para>
/// would be represented in the AST as a <see cref="DeclarationNode"/> with:
/// * The <see cref="Name"/> property set to <c>LovesickMaidens</c> as before.
/// * The <see cref="Type"/> property set to <see cref="LiteralType"/><c>.Integer</c> as before.
/// * The <see cref="InitialValue"/> property set to a <see cref="PrefixExpressionNode"/>, as a function call uses the <see cref="Operator"/><c>.Summon</c> operator, with:
///     * The <see cref="PrefixExpressionNode.Operator"/> property set to <see cref="Operator"/><c>.Summon</c>.
///     * The <see cref="PrefixExpressionNode.Arguments"/> property set to a list containing a single <see cref="IdentifierNode"/> with the name of the function being called.
/// </para>
/// <para>
/// and when parsed would be represented in the AST as:
/// </para>
/// <code>
/// new DeclarationNode()
/// {
///     Name = "LovesickMaidens",
///     Type = LiteralType.Integer,
///     InitialValue = new PrefixExpressionNode()
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
public class DeclarationNode : Statement
{
    /// <summary>
    /// Gets or initialises the name of the variable being declared.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or initialises the type of the variable.
    /// </summary>
    public required LiteralType Type { get; init; }

    /// <summary>
    /// Gets or initialises the optional initial value.
    /// </summary>
    public Expression? InitialValue { get; init; }
}
