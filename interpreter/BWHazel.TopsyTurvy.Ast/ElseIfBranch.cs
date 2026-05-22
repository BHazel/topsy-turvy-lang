using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a single else-if branch.
/// </summary>
/// <param name="Condition">The condition to evaluate for this branch.</param>
/// <param name="Block">The block of statements to execute if the condition is true.</param>
public record ElseIfBranch(Expression? Condition, IReadOnlyList<Statement> Block);
