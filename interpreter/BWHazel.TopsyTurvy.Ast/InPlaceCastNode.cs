namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a cast that mutates the variable in place.
/// </summary>
public class InPlaceCastNode : TypeCastNode
{
    /// <summary>
    /// Gets or initialises the name of the variable to cast.
    /// </summary>
    public required string Target { get; init; }
    
    /// <summary>
    /// Gets or initialises the destination type.
    /// </summary>
    public required TopsyTurvyLiteralType NewType { get; init; }
}
