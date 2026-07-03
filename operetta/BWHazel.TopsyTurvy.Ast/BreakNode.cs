namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a break statement.
/// </summary>
/// <remarks>
/// <para>
/// This corresponds to the <c>THAT WILL DO.</c> statement in Topsy Turvy.  It is used to exit from a <c>BY A LEGAL FICTION</c> loop or
/// <c>IN WHICH CAPACITY?</c> switch block.
/// </para>
/// <para>
/// For example, the following code in Topsy Turvy to exit an infinite loop:
/// </para>
/// <code>
/// BY A LEGAL FICTION WHILST VERITY
///     BEHOLD "The Lord High Executioner!"
///     THAT WILL DO.
/// THE TERM EXPIRES.
/// </code>
/// <para>
/// the <c>THAT WILL DO.</c> statement would be represented in the AST as a <see cref="BreakNode"/>, within the <see cref="LoopNode"/>:
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
///         new BreakNode() { /* ... */ }
///     ],
///     Span = new() { /* ... */ }
/// };
/// </code>
/// <para>
/// Please note examples have commented out sections for brevity.
/// </para>
/// </remarks>
public class BreakNode : Statement
{
}
