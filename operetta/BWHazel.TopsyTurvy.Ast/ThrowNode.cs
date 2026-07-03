namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a throw statement.
/// </summary>
/// <remarks>
/// <para>
/// This corresponds to the <c>A HIDEOUS CURSE ON</c> throw statement in Topsy Turvy.  It raises an exception,
/// carrying a <see cref="Value"/> expression as the exception payload.  If uncaught by a surrounding
/// <see cref="TryCatchNode"/>, the programme terminates with an error and the exception payload value is reported.
/// </para>
/// <para>
/// For example, the following code in Topsy Turvy raises an exception with a string payload:
/// </para>
/// <code>
/// A HIDEOUS CURSE ON "His Solicitor"
/// </code>
/// <para>
/// would be represented in the AST as a <see cref="ThrowNode"/> with:
/// * The <see cref="Value"/> property set to a <see cref="LiteralNode"/> with <see cref="LiteralNode.Type"/> set to
///   <see cref="LiteralType"/>.<c>String</c> and <see cref="LiteralNode.Value"/> set to <c>His Solicitor</c>.
/// </para>
/// <para>
/// When parsed this would be represented in the AST as:
/// </para>
/// <code>
/// new ThrowNode()
/// {
///     Value = new LiteralNode()
///     {
///         Type = LiteralType.String,
///         Value = "His Solicitor",
///         Span = new() { /* ... */ }
///     },
///     Span = new() { /* ... */ }
/// };
/// </code>
/// <para>
/// Please note examples have commented out sections for brevity.
/// </para>
/// </remarks>
public class ThrowNode : Statement
{
    /// <summary>
    /// Gets or initialises the expression whose value is carried as the exception payload.
    /// </summary>
    public required Expression Value { get; init; }
}
