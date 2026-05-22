namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a cast that produces a value without mutating the variable.
/// </summary>
public class ExpressionCastNode : TypeCastNode
{
    /// <summary>
    /// Gets or initialises the expression to cast.
    /// </summary>
    public required Expression Expression { get; init; }

    /// <summary>
    /// Gets or initialises the destination type.
    /// </summary>
    public required LiteralType NewType { get; init; }
}
