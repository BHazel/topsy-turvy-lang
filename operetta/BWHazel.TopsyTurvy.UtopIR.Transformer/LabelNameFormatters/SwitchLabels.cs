using System.Collections.Generic;
using BWHazel.TopsyTurvy.UtopIR.Ast;

namespace BWHazel.TopsyTurvy.UtopIR.Transformer.LabelNameFormatters;

/// <summary>
/// Bundles the labels needed to lower a switch block.
/// </summary>
/// <param name="CaseLabels">The labels branched to for each case, in the same order as the switch cases.</param>
/// <param name="DefaultLabel">The label branched to when no case matches.</param>
/// <param name="ClosingLabel">The label reached once the chosen case or default block completes.</param>
public sealed record SwitchLabels(IReadOnlyList<UtopIRLabel> CaseLabels, UtopIRLabel DefaultLabel, UtopIRLabel ClosingLabel);
