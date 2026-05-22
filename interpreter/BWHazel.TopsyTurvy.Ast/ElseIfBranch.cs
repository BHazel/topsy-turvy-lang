using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a single else-if branch.
/// </summary>
public record ElseIfBranch(TopsyTurvyExpression Condition, IReadOnlyList<TopsyTurvyStatement> Block);
