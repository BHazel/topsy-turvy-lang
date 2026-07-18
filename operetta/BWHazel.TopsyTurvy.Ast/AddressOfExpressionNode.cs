namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents an address-of expression that resolves a pointer to an existing variable.
/// </summary>
/// <remarks>
/// <para>
/// This corresponds to the <c>GALLERY PICTURE TO &lt;variable&gt;</c> expression in Topsy Turvy.  <c>&lt;variable&gt;</c>
/// must be the name of an already-declared variable: literals and arbitrary expressions are not valid.
/// </para>
/// <para>
/// When the target variable is a scalar, the resulting pointer refers to that variable directly.  When the target is
/// an array (<c>A LITTLE LIST OF &lt;type&gt;</c>) or a <c>YARN</c>, the pointer points to the first element or
/// character, enabling subsequent pointer arithmetic.
/// </para>
/// <para>
/// This expression is only valid as the <see cref="PointerDeclarationNode.InitialValue"/> of a pointer declaration or
/// the <see cref="AssignmentNode.Value"/> of an assignment to an existing pointer variable; using it anywhere else is a
/// type-checker error.
/// </para>
/// <para>
/// For example, the following code in Topsy Turvy to assign a pointer <c>NumberPointer</c> to point at the variable
/// <c>Number</c>:
/// </para>
/// <code>
/// NumberPointer IS APPOINTED GALLERY PICTURE TO Number
/// </code>
/// <para>
/// would be represented in the AST as an <see cref="AssignmentNode"/> with its <see cref="AssignmentNode.Value"/> set
/// to an <see cref="AddressOfExpressionNode"/> with:
/// * The <see cref="VariableName"/> property set to <c>Number</c>.
/// </para>
/// <para>
/// When parsed this would be represented in the AST as an <see cref="AssignmentNode"/> wrapping:
/// </para>
/// <code>
/// new AddressOfExpressionNode()
/// {
///     VariableName = "Number",
///     Span = new() { /* ... */ }
/// };
/// </code>
/// <para>
/// Please note examples have commented out sections for brevity.
/// </para>
/// </remarks>
public class AddressOfExpressionNode : Expression
{
    /// <summary>
    /// Gets or initialises the name of the variable to point at.
    /// </summary>
    public required string VariableName { get; init; }
}
