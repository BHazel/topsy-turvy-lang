namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents an input statement.
/// </summary>
public class InputNode : Statement
{
    /// <summary>
    /// Gets or initialises the variable to store the input in.
    /// </summary>
    public required string Target { get; init; }
}
