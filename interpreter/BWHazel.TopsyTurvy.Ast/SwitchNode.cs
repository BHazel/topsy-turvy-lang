using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a switch statement.
/// </summary>
public class SwitchNode : Statement
{
    /// <summary>
    /// Gets or initialises the expression to switch on.
    /// </summary>
    public Expression? Expression { get; init; }

    /// <summary>
    /// Gets or initialises the list of cases.
    /// </summary>
    public required IReadOnlyList<SwitchCase> Cases { get; init; }

    /// <summary>
    /// Gets or initialises the default block to execute if no case matches.
    /// </summary>
    public IReadOnlyList<Statement> DefaultBlock { get; init; } = new List<Statement>().AsReadOnly();
}
