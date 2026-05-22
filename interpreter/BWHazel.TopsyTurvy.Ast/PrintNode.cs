namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a print statement.
/// </summary>
public class PrintNode : TopsyTurvyStatement
{
    /// <summary>
    /// Gets or initialises the expression to print.
    /// </summary>
    public required TopsyTurvyExpression Expression { get; init; }

    /// <summary>
    /// Gets or initialises a value indicating whether to suppress the trailing newline.
    /// </summary>
    public bool SuppressNewline { get; init; }
}
