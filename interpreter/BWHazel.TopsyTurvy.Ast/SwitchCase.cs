using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a single switch case.
/// </summary>
/// <param name="Literal">The literal value for the case.</param>
/// <param name="Block">The block of statements to execute if the case matches.</param>
/// <param name="HasBreak">Indicates whether the case block ends with a break statement.</param>
public record SwitchCase(object? Literal, IReadOnlyList<Statement> Block, bool HasBreak);
