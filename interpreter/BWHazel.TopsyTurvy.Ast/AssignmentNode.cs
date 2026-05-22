namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a variable assignment.
/// </summary>
public class AssignmentNode : TopsyTurvyStatement
{
    /// <summary>
    /// Gets or initialises the name of the variable to assign to.
    /// </summary>
    public required string Target { get; init; }

    /// <summary>
    /// Gets or initialises the value to assign.
    /// </summary>
    public required TopsyTurvyExpression Value { get; init; }
}
