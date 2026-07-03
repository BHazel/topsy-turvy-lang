namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// A statement node that wraps a standalone expression.
/// </summary>
/// <remarks>
/// <para>
/// This represents a statement that consists solely of an expression, often when an expression has a side-effect of its own.
/// Examples include:
/// * A function call such as <c>SUMMON HowManyPoets WITH NOTHING IF YOU PLEASE.</c>.
/// * A stand-alone evaluation and assignment into the implicit <c>JUST SO</c> variable such as <c>SUM OF ConservativePeers AND LiberalPeers</c>.
/// Expression statements can be considered the bridge between expressions and statements.
/// </para>
/// <para>
/// Topsy Turvy syntax can map to an <see cref="ExpressionStatement"/> when an expression is used as a statement, such as in the examples above.
/// </para>
/// </remarks>
public class ExpressionStatement : Statement
{
    /// <summary>
    /// Gets or initialises the expression being used as a statement.
    /// </summary>
    public required Expression Expression { get; init; }
}
