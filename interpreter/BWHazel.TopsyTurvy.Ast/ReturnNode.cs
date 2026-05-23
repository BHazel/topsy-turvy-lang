namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a return statement inside a function body.
/// </summary>
/// <remarks>
/// This covers both return statements with a value and the closing statement.
/// </remarks>
public class ReturnNode : Statement
{
    /// <summary>
    /// Gets or initialises the expression whose value is returned, or <c>null</c> for a
    /// function without a return value.
    /// </summary>
    public Expression? Value { get; init; }
}
