using System.Collections.Generic;
using BWHazel.TopsyTurvy.UtopIR.Ast;

namespace BWHazel.TopsyTurvy.UtopIR.Transformer.LabelNameFormatters;

/// <summary>
/// Bundles the labels needed to lower a conditional or ternary expression.
/// </summary>
/// <remarks>
/// Shared by both constructs, since a ternary has the same true/else/closing shape as a
/// conditional with no else-if branches but its <see cref="ElseIfLabels"/> is always empty.
/// </remarks>
/// <param name="TrueLabel">The label branched to when the condition is true.</param>
/// <param name="ElseIfLabels">The labels branched to for each <c>OR, IF NOT,</c> branch, in order.</param>
/// <param name="ElseLabel">The label branched to when every condition is false.</param>
/// <param name="ClosingLabel">The label reached once the chosen branch completes.</param>
public sealed record ConditionalLabels(UtopIRLabel TrueLabel, IReadOnlyList<UtopIRLabel> ElseIfLabels, UtopIRLabel ElseLabel, UtopIRLabel ClosingLabel);
