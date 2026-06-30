namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents an array element access expression.
/// </summary>
/// <remarks>
/// <para>
/// This corresponds to the <c>VICTIM &lt;index&gt; ON &lt;array&gt;</c> expression in Topsy Turvy.  The index is 1-based
/// where <c>VICTIM 1</c> refers to the first element.  Accessing an out-of-range index is a runtime error.
/// </para>
/// <para>
/// As an <see cref="Expression"/>, <c>VICTIM</c> may appear anywhere a value is expected such as the right-hand side of
/// <c>IS APPOINTED</c>, as the <c>BEING</c> initialiser of a declaration, as an argument to a function call, or as a
/// sub-expression.  When used as a standalone statement, the result is stored in the implicit <c>JUST SO</c> variable via
/// the <c>ExpressionStatement</c> path.
/// </para>
/// <para>
/// For example, the following code in Topsy Turvy to read the first element of the array <c>miscreants</c>:
/// </para>
/// <code>
/// BEHOLD VICTIM 1 ON miscreants
/// </code>
/// <para>
/// would be represented in the AST as a <see cref="PrintNode"/> with its <see cref="PrintNode.Expression"/> set to an
/// <see cref="ArrayIndexNode"/> with:
/// * The <see cref="Index"/> property set to a <see cref="LiteralNode"/> for the integer value <c>1</c>.
/// * The <see cref="ArrayName"/> property set to <c>miscreants</c>.
/// </para>
/// <para>
/// When parsed this would be represented in the AST as a <see cref="PrintNode"/> wrapping:
/// </para>
/// <code>
/// new ArrayIndexNode()
/// {
///     Index = new LiteralNode()
///     {
///         Type = LiteralType.Integer,
///         Value = 1,
///         Span = new() { /* ... */ }
///     },
///     ArrayName = "miscreants",
///     Span = new() { /* ... */ }
/// };
/// </code>
/// <para>
/// Please note examples have commented out sections for brevity.
/// </para>
/// </remarks>
public class ArrayIndexNode : Expression
{
    /// <summary>
    /// Gets or initialises the expression that evaluates to the 1-based element index.
    /// </summary>
    public required Expression Index { get; init; }

    /// <summary>
    /// Gets or initialises the name of the array variable to index into.
    /// </summary>
    public required string ArrayName { get; init; }
}
