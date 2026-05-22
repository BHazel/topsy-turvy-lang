using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a conditional statement.
/// </summary>
public class ConditionalNode : TopsyTurvyStatement
{
    /// <summary>
    /// Gets or initialises the condition to evaluate.
    /// </summary>
    public required TopsyTurvyExpression Condition { get; init; }

    /// <summary>
    /// Gets or initialises the block of statements to execute if the condition is true.
    /// </summary>
    public required IReadOnlyList<TopsyTurvyStatement> TrueBlock { get; init; }

    /// <summary>
    /// Gets or initialises the list of else-if branches.
    /// </summary>
    public IReadOnlyList<ElseIfBranch> ElseIfs { get; init; } = new List<ElseIfBranch>().AsReadOnly();
    
    /// <summary>
    /// Gets or initialises the block to execute if all conditions were false.
    /// </summary>
    public IReadOnlyList<TopsyTurvyStatement> ElseBlock { get; init; } = new List<TopsyTurvyStatement>().AsReadOnly();
}
