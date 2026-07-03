namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents an array element assignment statement.
/// </summary>
/// <remarks>
/// <para>
/// This corresponds to the <c>VICTIM &lt;index&gt; ON &lt;array&gt; IS APPOINTED &lt;value&gt;</c> statement in Topsy Turvy.
/// The index is 1-based where <c>VICTIM 1</c> refers to the first element.  Assigning to an out-of-range index or to a
/// <c>CONSERVATIVE</c> array is a runtime error.
/// </para>
/// <para>
/// For example, the following code in Topsy Turvy to replace the second element of the array <c>miscreants</c>:
/// </para>
/// <code>
/// VICTIM 2 ON miscreants IS APPOINTED "Nanki-Poo"
/// </code>
/// <para>
/// would be represented in the AST as an <see cref="ArrayElementAssignmentNode"/> with:
/// * The <see cref="Index"/> property set to a <see cref="LiteralNode"/> for the integer value <c>2</c>.
/// * The <see cref="ArrayName"/> property set to <c>miscreants</c>.
/// * The <see cref="Value"/> property set to a <see cref="LiteralNode"/> for the string value <c>Nanki-Poo</c>.
/// </para>
/// <para>
/// When parsed this would be represented in the AST as:
/// </para>
/// <code>
/// new ArrayElementAssignmentNode()
/// {
///     Index = new LiteralNode()
///     {
///         Type = LiteralType.Integer,
///         Value = 2,
///         Span = new() { /* ... */ }
///     },
///     ArrayName = "miscreants",
///     Value = new LiteralNode()
///     {
///         Type = LiteralType.String,
///         Value = "Nanki-Poo",
///         Span = new() { /* ... */ }
///     },
///     Span = new() { /* ... */ }
/// };
/// </code>
/// <para>
/// Please note examples have commented out sections for brevity.
/// </para>
/// </remarks>
public class ArrayElementAssignmentNode : Statement
{
    /// <summary>
    /// Gets or initialises the expression that evaluates to the 1-based element index.
    /// </summary>
    public required Expression Index { get; init; }

    /// <summary>
    /// Gets or initialises the name of the array variable whose element is being replaced.
    /// </summary>
    public required string ArrayName { get; init; }

    /// <summary>
    /// Gets or initialises the expression that evaluates to the new element value.
    /// </summary>
    public required Expression Value { get; init; }
}
