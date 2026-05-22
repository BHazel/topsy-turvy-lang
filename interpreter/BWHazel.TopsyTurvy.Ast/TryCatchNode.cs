using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a try-catch block.
/// </summary>
public class TryCatchNode : TopsyTurvyStatement
{
    /// <summary>
    /// Gets or initialises the operation wrapped in the try block.
    /// </summary>
    public required TopsyTurvyExpression Operation { get; init; }

    /// <summary>
    /// Gets or initialises the block of statements to execute on success.
    /// </summary>
    public required IReadOnlyList<TopsyTurvyStatement> SuccessBlock { get; init; }

    /// <summary>
    /// Gets or initialises the block of statements to execute on exception.
    /// </summary>
    public required IReadOnlyList<TopsyTurvyStatement> ExceptionBlock { get; init; }
}
