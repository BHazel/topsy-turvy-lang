using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a function definition.
/// </summary>
public class FunctionDefinitionNode : Statement
{
    /// <summary>
    /// Gets or initialises the name of the function.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or initialises the list of parameters.
    /// </summary>
    public required IReadOnlyList<string> Parameters { get; init; }

    /// <summary>
    /// Gets or initialises the block of statements in the function body.
    /// </summary>
    public required IReadOnlyList<Statement> Body { get; init; }

    /// <summary>
    /// Gets or initialises the value returned by the function, if any.
    /// </summary>
    public Expression? ReturnValue { get; init; }
}
