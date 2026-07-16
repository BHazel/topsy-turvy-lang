namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a write-through assignment via a pointer dereference.
/// </summary>
/// <remarks>
/// <para>
/// This corresponds to the <c>VIEW FROM &lt;pointer&gt; IS APPOINTED &lt;value&gt;</c> statement in Topsy Turvy.  It
/// replaces the value currently referred to by the pointer.  Writing through an unassigned (<c>NAUGHT</c>) pointer, or
/// through a pointer that refers to a character within a <c>YARN</c>, is a runtime error since strings are immutable.
/// </para>
/// <para>
/// For example, the following code in Topsy Turvy to assign a new value to the variable pointed at by
/// <c>NumberPointer</c>:
/// </para>
/// <code>
/// VIEW FROM NumberPointer IS APPOINTED 23
/// </code>
/// <para>
/// would be represented in the AST as a <see cref="DereferenceAssignmentNode"/> with:
/// * The <see cref="PointerName"/> property set to <c>NumberPointer</c>.
/// * The <see cref="Value"/> property set to a <see cref="LiteralNode"/> for the integer value <c>23</c>.
/// </para>
/// <para>
/// When parsed this would be represented in the AST as:
/// </para>
/// <code>
/// new DereferenceAssignmentNode()
/// {
///     PointerName = "NumberPointer",
///     Value = new LiteralNode()
///     {
///         Type = LiteralType.Integer,
///         Value = 23,
///         Span = new() { /* ... */ }
///     },
///     Span = new() { /* ... */ }
/// };
/// </code>
/// <para>
/// Please note examples have commented out sections for brevity.
/// </para>
/// </remarks>
public class DereferenceAssignmentNode : Statement
{
    /// <summary>
    /// Gets or initialises the name of the pointer variable to write through.
    /// </summary>
    public required string PointerName { get; init; }

    /// <summary>
    /// Gets or initialises the expression that evaluates to the new value.
    /// </summary>
    public required Expression Value { get; init; }
}
