namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a variable name or function identifier in the AST.
/// </summary>
public class IdentifierNode : TopsyTurvyExpression
{
    /// <summary>
    /// Gets or initialises the name of the identifier.
    /// </summary>
    public required string Name { get; init; }
}
