namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a statement that reads a line of input from standard input into a variable.
/// </summary>
/// <remarks>
/// <para>
/// This corresponds to the <c>PRAY TELL</c> input statement in Topsy Turvy.  It reads a line from standard input and stores
/// it in the named variable as a <c>YARN</c> (string).  Every input statement consists of a single <see cref="Target"/> naming
/// the variable that receives the input.  The target variable must be of type <c>YARN</c>.  If the input needs to be converted to
/// another type, use an <see cref="ExpressionCastNode"/> (<c>AS IT WERE &lt;var&gt; AS A &lt;type&gt;</c>) in a subsequent assignment.
/// </para>
/// <para>
/// For example, the following code in Topsy Turvy reads a line of input into the variable <c>Incantation</c>:
/// </para>
/// <code>
/// PRAY TELL Incantation
/// </code>
/// <para>
/// would be represented in the AST as an <see cref="InputNode"/> with:
/// * The <see cref="Target"/> property set to <c>Incantation</c>.
/// </para>
/// <para>
/// When parsed this would be represented in the AST as:
/// </para>
/// <code>
/// new InputNode()
/// {
///     Target = "Incantation",
///     Span = new() { /* ... */ }
/// };
/// </code>
/// <para>
/// Please note examples have commented out sections for brevity.
/// </para>
/// </remarks>
public class InputNode : Statement
{
    /// <summary>
    /// Gets or initialises the variable to store the input in.
    /// </summary>
    public required string Target { get; init; }
}
