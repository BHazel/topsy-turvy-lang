using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Parser;

/// <summary>
/// Defines the type of a loop and its associated data.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="LoopDefinition"/> is an intermediate parse result that captures the type-specific details of a loop's opening
/// modifier clause — the part between the optional label and the loop body.  It is produced by the parser when it recognises
/// the <c>ASCENDING</c>, <c>DESCENDING</c>, <c>WHILST</c>, or absent modifier, and its fields are then used to populate the
/// corresponding <see cref="LoopNode"/> properties.  It is not itself a <see cref="Node"/> and does not appear in the finished AST.
/// </para>
/// <para>
/// The three fields map directly onto <see cref="LoopNode"/>: <see cref="Type"/> becomes <see cref="LoopNode.Type"/>,
/// <see cref="Condition"/> becomes <see cref="LoopNode.Condition"/>, and <see cref="Variable"/> becomes
/// <see cref="LoopNode.LoopVariable"/>.  For <see cref="LoopType.Infinite"/> loops both <see cref="Condition"/> and
/// <see cref="Variable"/> are <c>null</c>; for <see cref="LoopType.Whilst"/> loops only <see cref="Variable"/> is <c>null</c>.
/// </para>
/// </remarks>
/// <param name="Type">The type of the loop.</param>
/// <param name="Condition">The condition for the loop, if applicable.</param>
/// <param name="Variable">The loop variable, if applicable.</param>
public record LoopDefinition(LoopType Type, Expression? Condition, string? Variable);
