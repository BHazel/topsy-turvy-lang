namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents an input statement.
/// </summary>
public class InputNode : TopsyTurvyStatement
{
    /// <summary>
    /// Gets or initialises the variable to store the input in.
    /// </summary>
    public required string Target { get; init; }
}
