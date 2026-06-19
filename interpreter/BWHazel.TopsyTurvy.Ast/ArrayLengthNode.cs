namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents an array length expression.
/// </summary>
/// <remarks>
/// <para>
/// This corresponds to the <c>RECKONING OF &lt;array&gt;</c> expression in Topsy Turvy.  It evaluates to the number of
/// elements in the array as a <c>PEER</c> (integer).  The result is always greater than or equal to 0.
/// </para>
/// <para>
/// For example, the following code in Topsy Turvy to read the length of the array <c>miscreants</c>:
/// </para>
/// <code>
/// BEHOLD RECKONING OF miscreants
/// </code>
/// <para>
/// would be represented in the AST as a <see cref="PrintNode"/> with its <see cref="PrintNode.Expression"/> set to an
/// <see cref="ArrayLengthNode"/> with:
/// * The <see cref="ArrayName"/> property set to <c>miscreants</c>.
/// </para>
/// <para>
/// When parsed this would be represented in the AST as a <see cref="PrintNode"/> wrapping:
/// </para>
/// <code>
/// new ArrayLengthNode()
/// {
///     ArrayName = "miscreants",
///     Span = new() { /* ... */ }
/// };
/// </code>
/// <para>
/// Please note examples have commented out sections for brevity.
/// </para>
/// </remarks>
public class ArrayLengthNode : Expression
{
    /// <summary>
    /// Gets or initialises the name of the array variable.
    /// </summary>
    public required string ArrayName { get; init; }
}
