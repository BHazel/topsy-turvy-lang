using BWHazel.TopsyTurvy.UtopIR.Ast;

namespace BWHazel.TopsyTurvy.UtopIR.Transformer.LabelNameFormatters;

/// <summary>
/// Bundles the labels needed to lower a loop.
/// </summary>
/// <remarks>
/// <see cref="ContinueLabel"/> is <c>null</c> for an infinite or Whilst loop, whose continue target is
/// <see cref="OpeningLabel"/> itself; it is populated only for an ascending or descending loop, whose
/// continue target must land after the body but before the increment or decrement step.
/// </remarks>
/// <param name="OpeningLabel">The label both entering the body and, for an infinite or Whilst loop, checking the condition.</param>
/// <param name="ContinueLabel">The continue target, or <c>null</c> when it is the same as <see cref="OpeningLabel"/>.</param>
/// <param name="ClosingLabel">The label reached when the loop ends.</param>
public sealed record LoopLabels(UtopIRLabel OpeningLabel, UtopIRLabel? ContinueLabel, UtopIRLabel ClosingLabel);
