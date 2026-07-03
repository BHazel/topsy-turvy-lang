namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a print statement.
/// </summary>
/// <remarks>
/// <para>
/// This corresponds to the <c>BEHOLD</c> output statement in Topsy Turvy.  It evaluates an <see cref="Expression"/>
/// and writes the result to standard output.  By default a new line is appended but this can be suppressed by adding
/// <c>WITHOUT CEREMONY</c>, which is reflected by the <see cref="SuppressNewline"/> property.
/// audience.
/// </para>
/// <para>
/// For example, the following code in Topsy Turvy prints a value followed by a new line:
/// </para>
/// <code>
/// BEHOLD "The Lord High Executioner!"
/// </code>
/// <para>
/// would be represented in the AST as a <see cref="PrintNode"/> with:
/// * The <see cref="Expression"/> property set to a <see cref="LiteralNode"/> with:
///     * The <see cref="LiteralNode.Type"/> property set to <see cref="LiteralType"/>.<c>String</c>.
///     * The <see cref="LiteralNode.Value"/> property set to <c>The Lord High Executioner!</c>.
/// * The <see cref="SuppressNewline"/> property set to <c>false</c>.
/// </para>
/// <para>
/// When parsed this would be represented in the AST as:
/// </para>
/// <code>
/// new PrintNode()
/// {
///     Expression = new LiteralNode()
///     {
///         Type = LiteralType.String,
///         Value = "The Lord High Executioner!",
///         Span = new() { /* ... */ }
///     },
///     SuppressNewline = false,
///     Span = new() { /* ... */ }
/// };
/// </code>
/// <para>
/// Adding <c>WITHOUT CEREMONY</c> produces the same node with <see cref="SuppressNewline"/> set to <c>true</c>.
/// </para>
/// <para>
/// Please note examples have commented out sections for brevity.
/// </para>
/// </remarks>
public class PrintNode : Statement
{
    /// <summary>
    /// Gets or initialises the expression to print.
    /// </summary>
    public required Expression Expression { get; init; }

    /// <summary>
    /// Gets or initialises a value indicating whether to suppress the trailing newline.
    /// </summary>
    public bool SuppressNewline { get; init; }
}
