using BWHazel.TopsyTurvy.UtopIR.Ast;

namespace BWHazel.TopsyTurvy.UtopIR.Transformer.LabelNameFormatters;

/// <summary>
/// Bundles the labels needed to lower a guard clause.
/// </summary>
/// <param name="ElseLabel">The label branched to when the guard condition fails.</param>
/// <param name="ClosingLabel">The label reached once the else branch, if taken, completes.</param>
public sealed record GuardLabels(UtopIRLabel ElseLabel, UtopIRLabel ClosingLabel);
