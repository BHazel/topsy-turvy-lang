namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Base class for all statement nodes in the AST.
/// </summary>
/// <remarks>
/// <para>
/// This represents a statement, a construct which performs an action when executed.  It stands alone and executes in sequence with
/// other statements and has side effects.  It is made up of <see cref="Expression"/>s which are inputs to the statement.  Every line
/// of Topsy Turvy is a statement.  Examples include:
/// * A variable assignment such as <c>PoemSubject IS APPOINTED "Hollow"</c>.
/// * A function declaration such as <c>IT IS MY DUTY TO PERFORM HowManyPoets UNDER NO OBLIGATION</c> ... <c>MY DUTY IS DISCHARGED.</c>.
/// * An in-place cast such as <c>Ko-Ko IS HENCEFORTH A PEER</c>.
/// Statements can be considered to do something.
/// </para>
/// <para>
/// There is no Topsy Turvy syntax that maps directly to <see cref="Statement"/>.  Instead this is a base class for all the
/// different statement types.
/// </para>
/// </remarks>
public abstract class Statement : Node
{
}
