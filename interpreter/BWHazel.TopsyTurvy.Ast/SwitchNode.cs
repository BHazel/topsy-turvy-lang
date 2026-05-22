using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a switch statement.
/// </summary>
public class SwitchNode : TopsyTurvyStatement
{
    /// <summary>
    /// Gets or initialises the expression to switch on.
    /// </summary>
    public required TopsyTurvyExpression Expression { get; init; }

    /// <summary>
    /// Gets or initialises the list of cases.
    /// </summary>
    public required IReadOnlyList<SwitchCase> Cases { get; init; }

    /// <summary>
    /// Gets or initialises the default block to execute if no case matches.
    /// </summary>
    public IReadOnlyList<TopsyTurvyStatement> DefaultBlock { get; init; } = new List<TopsyTurvyStatement>().AsReadOnly();
}
