namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a reference to a named variable or function in the AST.
/// </summary>
/// <remarks>
/// <para>
/// This represents a variable or function by name when used in Topsy Turvy.  Every identifier consists of a single
/// <see cref="Name"/> string.  The implicit variable <c>JUST SO</c> is also represented as an <see cref="IdentifierNode"/> with
/// the <see cref="Name"/> set to <c>JUST SO</c>.
/// </para>
/// <para>
/// For example, the following code in Topsy Turvy prints the value of a variable <c>PoemSubject</c>:
/// </para>
/// <code>
/// BEHOLD PoemSubject
/// </code>
/// <para>
/// would be represented in the AST as a <see cref="PrintNode"/> with its <see cref="PrintNode.Expression"/> set to an
/// <see cref="IdentifierNode"/> with:
/// * The <see cref="Name"/> property set to <c>PoemSubject</c>.
/// </para>
/// <para>
/// When parsed this would be represented in the AST as:
/// </para>
/// <code>
/// new PrintNode()
/// {
///     Expression = new IdentifierNode()
///     {
///         Name = "PoemSubject",
///         Span = new() { /* ... */ }
///     },
///     Span = new() { /* ... */ }
/// };
/// </code>
/// <para>
/// Please note examples have commented out sections for brevity.
/// </para>
/// </remarks>
public class IdentifierNode : Expression
{
    /// <summary>
    /// Gets or initialises the name of the identifier.
    /// </summary>
    public required string Name { get; init; }
}
