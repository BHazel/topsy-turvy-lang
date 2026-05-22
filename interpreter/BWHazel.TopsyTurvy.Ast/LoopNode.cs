using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a loop statement.
/// </summary>
public class LoopNode : TopsyTurvyStatement
{
    /// <summary>
    /// Gets or initialises the optional label for the loop.
    /// </summary>
    public string? Label { get; init; }

    /// <summary>
    /// Gets or initialises the type of the loop.
    /// </summary>
    public required LoopType Type { get; init; }

    /// <summary>
    /// Gets or initialises the exit or continuation condition.
    /// </summary>
    public TopsyTurvyExpression? Condition { get; init; }

    /// <summary>
    /// Gets or initialises the variable used for counting in ascending/descending loops.
    /// </summary>
    public string? LoopVariable { get; init; }
    
    /// <summary>
    /// Gets or initialises the body of the loop.
    /// </summary>
    public required IReadOnlyList<TopsyTurvyStatement> Body { get; init; }
}
