using System.Collections.Generic;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.UtopIR.Transformer.LabelNameFormatters;

/// <summary>
/// Defines methods for creating label names <see cref="TopsyTurvyToUtopIRTransformer"/> generates
/// when lowering Topsy Turvy control-flow statements into UtopIR.
/// </summary>
/// <remarks>
/// One dispatch method exists per control-flow construct family, mirroring how
/// <see cref="TopsyTurvyToUtopIRTransformer"/> itself dispatches on AST node type.  Each method
/// returns a small record bundling every label its construct needs.
/// </remarks>
public interface ILabelNameFormatter
{
    /// <summary>
    /// Formats one or more raw text parts into a single label text segment, for example condition
    /// or case text embedded within a label.
    /// </summary>
    /// <param name="parts">The raw parts to join.</param>
    /// <returns>The parts joined and formatted according to a naming scheme.</returns>
    string FormatText(params string[] parts);

    /// <summary>
    /// Formats the labels needed to lower a conditional.
    /// </summary>
    /// <param name="node">The conditional being lowered.</param>
    /// <param name="conditionText">The formatted condition text.</param>
    /// <param name="elseIfConditionTexts">The formatted condition text for each else-if branch, in order.</param>
    /// <returns>The labels for this conditional.</returns>
    ConditionalLabels ForConditional(ConditionalNode node, string conditionText, IReadOnlyList<string> elseIfConditionTexts);

    /// <summary>
    /// Formats the labels needed to lower a ternary expression.
    /// </summary>
    /// <param name="node">The ternary expression being lowered.</param>
    /// <param name="conditionText">The formatted condition text.</param>
    /// <returns>The labels for this ternary expression.</returns>
    ConditionalLabels ForConditional(TernaryExpressionNode node, string conditionText);

    /// <summary>
    /// Formats the labels needed to lower a guard clause.
    /// </summary>
    /// <param name="node">The guard clause being lowered.</param>
    /// <returns>The labels for this guard clause.</returns>
    GuardLabels ForGuard(GuardNode node);

    /// <summary>
    /// Formats the labels needed to lower a switch block.
    /// </summary>
    /// <param name="node">The switch block being lowered.</param>
    /// <param name="caseTexts">The formatted case value text for each case, in the same order as the switch cases.</param>
    /// <returns>The labels for this switch block.</returns>
    SwitchLabels ForSwitch(SwitchNode node, IReadOnlyList<string> caseTexts);

    /// <summary>
    /// Formats the labels needed to lower a loop.
    /// </summary>
    /// <param name="node">The loop being lowered, of any <see cref="LoopType"/>.</param>
    /// <param name="conditionText">The formatted loop condition text, or <c>null</c> for an infinite loop.</param>
    /// <returns>The labels for this loop.</returns>
    LoopLabels ForLoop(LoopNode node, string? conditionText);
}
