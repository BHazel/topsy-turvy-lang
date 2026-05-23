using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a single switch case.
/// </summary>
/// <param name="Literal">The literal value for the case, or <c>null</c> for a fall-through label with no distinct value.</param>
/// <param name="Block">The block of statements to execute if the case matches. A <see cref="BreakNode"/> anywhere in the block prevents fall-through to subsequent cases.</param>
public record SwitchCase(object? Literal, IReadOnlyList<Statement> Block);
