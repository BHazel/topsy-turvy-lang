namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a literal value in the AST.
/// </summary>
public class LiteralNode : TopsyTurvyExpression
{
    /// <summary>
    /// Gets or initialises the actual value of the literal.
    /// </summary>
    public required object Value { get; init; }
    
    /// <summary>
    /// Gets or initialises the type of the literal.
    /// </summary>
    public required TopsyTurvyLiteralType Type { get; init; }
}
