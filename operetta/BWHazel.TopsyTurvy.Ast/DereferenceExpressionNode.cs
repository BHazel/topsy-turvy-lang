namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a pointer dereference expression.
/// </summary>
/// <remarks>
/// <para>
/// This corresponds to the <c>VIEW FROM &lt;pointer&gt;</c> expression in Topsy Turvy.  It evaluates to the value
/// currently referred to by the pointer.  Dereferencing an unassigned (<c>NAUGHT</c>) pointer is a runtime error.
/// </para>
/// <para>
/// As an <see cref="Expression"/>, <c>VIEW FROM</c> may appear anywhere a value is expected, such as the right-hand
/// side of <c>IS APPOINTED</c>, as the <c>BEING</c> initialiser of a declaration, as an argument to a function call, or
/// as a sub-expression.
/// </para>
/// <para>
/// For example, the following code in Topsy Turvy to read the value pointed at by <c>NumberPointer</c>:
/// </para>
/// <code>
/// PRAY WELCOME Number2 IS APPOINTED VIEW FROM NumberPointer
/// </code>
/// <para>
/// would be represented in the AST as a <see cref="DeclarationNode"/> with its <see cref="DeclarationNode.InitialValue"/>
/// set to a <see cref="DereferenceExpressionNode"/> with:
/// * The <see cref="PointerName"/> property set to <c>NumberPointer</c>.
/// </para>
/// <para>
/// When parsed this would be represented in the AST as a <see cref="DeclarationNode"/> wrapping:
/// </para>
/// <code>
/// new DereferenceExpressionNode()
/// {
///     PointerName = "NumberPointer",
///     Span = new() { /* ... */ }
/// };
/// </code>
/// <para>
/// Please note examples have commented out sections for brevity.
/// </para>
/// </remarks>
public class DereferenceExpressionNode : Expression
{
    /// <summary>
    /// Gets or initialises the name of the pointer variable to dereference.
    /// </summary>
    public required string PointerName { get; init; }
}
