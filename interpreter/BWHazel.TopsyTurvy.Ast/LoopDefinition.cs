namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Defines the type of a loop and its associated data.
/// </summary>
/// <param name="Type">The type of the loop.</param>
/// <param name="Condition">The condition for the loop, if applicable.</param>
/// <param name="Variable">The loop variable, if applicable.</param>
public record LoopDefinition(LoopType Type, Expression? Condition, string? Variable);
