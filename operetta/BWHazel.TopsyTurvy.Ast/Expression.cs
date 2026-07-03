namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Base class for all expression nodes in the AST.
/// </summary>
/// <remarks>
/// <para>
/// This represents an expression, a construct which produces a value when evaluated.  It never stands alone and is always part
/// of, and input to, a <see cref="Statement"/>.  Examples include:
/// * A variable or literal (on its own) such as <c>PoemSubject</c> or <c>20</c>.
/// * An operation such as <c>SUM OF ConservativePeers AND LiberalPeers</c>.
/// Expressions can be considered to answer the question "What value does this produce?"
/// </para>
/// <para>
/// There is no Topsy Turvy syntax that maps directly to <see cref="Expression"/>.  Instead this is a base class for all the
/// different expression types.
/// </para>
/// </remarks>
public abstract class Expression : Node
{
}
