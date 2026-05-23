namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a throw statement.
/// </summary>
public class ThrowNode : Statement
{
    /// <summary>
    /// Gets or initialises the expression whose value is carried as the exception payload.
    /// </summary>
    public required Expression Value { get; init; }
}
