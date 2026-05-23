using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Parser;

/// <summary>
/// A statement node that wraps a standalone expression.
/// </summary>
public class ExpressionStatement : Statement
{
    /// <summary>
    /// Gets or initialises the expression being used as a statement.
    /// </summary>
    public required Expression Expression { get; init; }
}
