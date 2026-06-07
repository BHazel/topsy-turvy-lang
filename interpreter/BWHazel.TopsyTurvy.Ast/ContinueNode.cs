namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a continue statement.
/// </summary>
/// <remarks>
/// <para>
/// This corresponds to the <c>ONCE MORE.</c> statement in Topsy Turvy.  It is used to continue to the next iteration of a <c>BY A LEGAL FICTION</c> loop.
/// </para>
/// <para>
/// For example, the following code in Topsy Turvy to continue an infinite loop:
/// </para>
/// <code>
/// BY A LEGAL FICTION WHILST VERITY
///     BEHOLD "Tarantara!"
///     ONCE MORE.
///     BEHOLD "Ra, ra, ra, ra!"
/// THE TERM EXPIRES.
/// </code>
/// <para>
/// the <c>ONCE MORE.</c> statement would be represented in the AST as a <see cref="ContinueNode"/>, within the <see cref="LoopNode"/>:
/// </para>
/// <code>
/// new LoopNode()
/// {
///     Label = null,
///     Type = LoopType.Whilst,
///     LoopVariable = null,
///     Condition = new LiteralNode() { /* ... */ },
///     Body =
///     [
///         new PrintNode() { /* ... */ },
///         new ContinueNode() { /* ... */ },
///         new PrintNode() { /* ... */ }
///     ],
///     Span = new() { /* ... */ }
/// };
/// </code>
/// <para>
/// Please note examples have commented out sections for brevity.
/// </para>
/// </remarks>
public class ContinueNode : Statement
{
}
