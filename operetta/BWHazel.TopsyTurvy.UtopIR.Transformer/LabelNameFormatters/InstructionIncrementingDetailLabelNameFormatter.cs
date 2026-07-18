using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.UtopIR.Ast;

namespace BWHazel.TopsyTurvy.UtopIR.Transformer.LabelNameFormatters;

/// <summary>
/// Names labels for control-flow constructs using descriptive names derived from the construct and operand details.
/// </summary>
/// <remarks>
/// <para>
/// A single counter is shared by every control-flow construct kind (conditional, ternary, guard,
/// switch, loop), incrementing once per <c>For*</c> call.  The number is not reset or tracked
/// separately per kind, so, for example, a loop containing a nested conditional numbers the loop
/// <c>1</c> and the conditional <c>2</c>, not <c>1</c> again.
/// </para>
/// <para>
/// <see cref="FormatText(string[])"/> joins its parts with <c>_</c>, then converts every character:
/// * The <c>£</c> sigil is dropped entirely and not replaced.
/// * A <c>.</c> between two digits (a decimal point within a floating-point literal) becomes <c>P</c>.
/// * Every other character that is not a letter, digit or underscore becomes <c>_</c>.
/// * Runs of consecutive <c>_</c> produced by this conversion collapse into one.
/// * The whole result is upper-cased.
/// 
/// For example,
/// * <c>FormatText("preadam", "Peer1", "Peer2")</c> returns <c>"PREADAM_PEER1_PEER2"</c>.
/// * <c>FormatText("alike", "2.5")</c> returns <c>"ALIKE_2P5"</c>.
/// </para>
/// <para>
/// Every <c>For*</c> method composes a construct-specific label set from a construct number and,
/// where relevant, already-formatted condition or case text:
/// * Conditionals and ternaries use a <c>T</c> prefix (<c>T[x]QS_.../T[x]OIN_.../T[x]O/T[x]SMFT</c>).
/// * Guards use a <c>G</c> prefix (<c>G[x]O/G[x]UO</c>).
/// * Switches use a <c>C</c> prefix (<c>C[x]AS_.../C[x]FAIL/C[x]NCBMS</c>).
/// * Loops use an <c>L</c> prefix, with <c>W</c>/<c>ASC</c>/<c>DESC</c> tags distinguishing the Whilst, ascending and descending kinds.
/// </para>
/// </remarks>
public sealed class InstructionIncrementingDetailLabelNameFormatter : ILabelNameFormatter
{
    private int counter;

    /// <inheritdoc/>
    public string FormatText(params string[] parts)
    {
        string joined = string.Join("_", parts);
        string converted = ConvertPunctuation(joined);
        string collapsed = CollapseUnderscores(converted);
        return collapsed.ToUpperInvariant();
    }

    /// <inheritdoc/>
    public ConditionalLabels ForConditional(ConditionalNode node, string conditionText, IReadOnlyList<string> elseIfConditionTexts)
    {
        int number = ++this.counter;
        IReadOnlyList<UtopIRLabel> elseIfLabels = [.. elseIfConditionTexts.Select(text => new UtopIRLabel($"T{number}OIN_{text}"))];

        return new(
            new($"T{number}QS_{conditionText}"),
            elseIfLabels,
            new($"T{number}O"),
            new($"T{number}SMFT"));
    }

    /// <inheritdoc/>
    public ConditionalLabels ForConditional(TernaryExpressionNode node, string conditionText)
    {
        int number = ++this.counter;
        return new(
            new($"T{number}QS_{conditionText}"),
            [],
            new($"T{number}O"),
            new($"T{number}SMFT"));
    }

    /// <inheritdoc/>
    public GuardLabels ForGuard(GuardNode node)
    {
        int number = ++this.counter;
        return new(new($"G{number}O"), new($"G{number}UO"));
    }

    /// <inheritdoc/>
    public SwitchLabels ForSwitch(SwitchNode node, IReadOnlyList<string> caseTexts)
    {
        int number = ++this.counter;
        IReadOnlyList<UtopIRLabel> caseLabels = [.. caseTexts.Select(text => new UtopIRLabel($"C{number}AS_{text}"))];

        return new(caseLabels, new($"C{number}FAIL"), new($"C{number}NCBMS"));
    }

    /// <inheritdoc/>
    public LoopLabels ForLoop(LoopNode node, string? conditionText)
    {
        int number = ++this.counter;
        string nameSuffix = this.NameSuffix(node.Label);

        return node.Type switch
        {
            LoopType.Infinite => new(
                new($"L{number}{nameSuffix}"),
                null,
                new($"L{number}TTE")),
            LoopType.Whilst => new(
                new($"L{number}W{nameSuffix}_{conditionText}"),
                null,
                new($"L{number}WTTE")),
            LoopType.Ascending or LoopType.Descending => this.ForCountedLoop(number, node.Type == LoopType.Ascending, nameSuffix, conditionText!),
            _ => throw new ArgumentOutOfRangeException(nameof(node), node.Type, "Unknown loop type.")
        };
    }

    /// <summary>
    /// Returns the labels for an ascending or descending loop, sharing the <c>ASC</c>/<c>DESC</c>
    /// tag logic between both kinds.
    /// </summary>
    /// <param name="number">The construct number already allocated for this loop.</param>
    /// <param name="ascending"><c>true</c> for an ascending loop, or <c>false</c> for a descending loop.</param>
    /// <param name="nameSuffix">The already-formatted <c>_[NAME]</c> suffix, or an empty string.</param>
    /// <param name="conditionText">The formatted loop condition text.</param>
    /// <returns>The labels for this counted loop.</returns>
    private LoopLabels ForCountedLoop(int number, bool ascending, string nameSuffix, string conditionText)
    {
        string tag = ascending
            ? "ASC"
            : "DESC";
        
        return new(
            new($"L{number}{tag}{nameSuffix}_{conditionText}"),
            new($"L{number}{tag}OM"),
            new($"L{number}{tag}TTE"));
    }

    /// <summary>
    /// Returns the <c>_[NAME]</c> label suffix for an optional loop name, or an empty string when
    /// no name is given.
    /// </summary>
    /// <param name="name">The raw, unformatted loop name, or <c>null</c>.</param>
    /// <returns>The formatted, underscore-prefixed suffix, or <see cref="string.Empty"/>.</returns>
    private string NameSuffix(string? name) =>
        name is null
            ? string.Empty
            : $"_{this.FormatText(name)}";

    /// <summary>
    /// Converts every character in the provided text per the punctuation rules of the label
    /// naming scheme.
    /// </summary>
    /// <param name="text">The raw, already-joined text to convert.</param>
    /// <returns>The converted text, not yet collapsed or upper-cased.</returns>
    private static string ConvertPunctuation(string text)
    {
        StringBuilder builder = new(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            char character = text[i];
            if (character == '£')
            {
                continue;
            }

            bool isDecimalPoint = character == '.'
                && i > 0
                && i < text.Length - 1
                && char.IsDigit(text[i - 1])
                && char.IsDigit(text[i + 1]);

            if (isDecimalPoint)
            {
                builder.Append('P');
                continue;
            }

            builder.Append(char.IsLetterOrDigit(character) || character == '_' ? character : '_');
        }

        return builder.ToString();
    }

    /// <summary>
    /// Collapses every run of consecutive <c>_</c> characters into a single <c>_</c>.
    /// </summary>
    /// <param name="text">The text to collapse.</param>
    /// <returns>The collapsed text.</returns>
    private static string CollapseUnderscores(string text) =>
        Regex.Replace(text, "_+", "_");
}
