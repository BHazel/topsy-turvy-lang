using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a try-catch block.
/// </summary>
public class TryCatchNode : Statement
{
    /// <summary>
    /// Gets or initialises the operation wrapped in the try block.
    /// </summary>
    public required Expression Operation { get; init; }

    /// <summary>
    /// Gets or initialises the block of statements to execute on success.
    /// </summary>
    public required IReadOnlyList<Statement> SuccessBlock { get; init; }

    /// <summary>
    /// Gets or initialises the block of statements to execute on exception.
    /// </summary>
    public required IReadOnlyList<Statement> ExceptionBlock { get; init; }
}
