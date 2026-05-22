using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a prefix expression in the AST.
/// </summary>
public class PrefixExpressionNode : Expression
{
    /// <summary>
    /// Gets or initialises the operator being applied.
    /// </summary>
    public required Operator Operator { get; init; }

    /// <summary>
    /// Gets or initialises the arguments (operands) of the expression.
    /// </summary>
    public required IReadOnlyList<Expression> Arguments { get; init; }
}
