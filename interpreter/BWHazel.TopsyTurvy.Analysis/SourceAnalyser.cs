using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace BWHazel.TopsyTurvy.Analysis;

/// <summary>
/// Provides source-text scanning utilities shared across analysis consumers.
/// </summary>
/// <remarks>
/// All methods are stateless and operate purely on source text.
/// </remarks>
public static class SourceAnalyser
{
    private static readonly Regex BlockCommentPattern =
        new(@"\(ASIDE, AT SOME LENGTH:.*?END OF ASIDE\.\)", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex StringLiteralPattern =
        new(@"""(?:[^""\\]|\\.)*""", RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex LineCommentPattern =
        new(@"ASIDE:.*", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>
    /// Returns absolute character offset ranges that must be excluded from source scanning.
    /// </summary>
    /// <param name="source">The full document source text.</param>
    /// <remarks>
    /// Covers block comments, string literals, and line comments in that priority order.
    /// </remarks>
    /// <returns>A list of start and end absolute offset pairs to skip.</returns>
    public static List<(int Start, int End)> FindSkipRanges(string source)
    {
        List<(int Start, int End)> ranges = [];
        foreach (Match match in BlockCommentPattern.Matches(source))
        {
            ranges.Add((match.Index, match.Index + match.Length));
        }

        foreach (Match match in StringLiteralPattern.Matches(source))
        {
            if (!IsInSkipRange(match.Index, ranges))
            {
                ranges.Add((match.Index, match.Index + match.Length));
            }
        }

        foreach (Match match in LineCommentPattern.Matches(source))
        {
            if (!IsInSkipRange(match.Index, ranges))
            {
                ranges.Add((match.Index, match.Index + match.Length));
            }
        }

        return ranges;
    }

    /// <summary>
    /// Builds an array of the absolute character offset at which each line begins.
    /// </summary>
    /// <param name="lines">The source lines.</param>
    /// <returns>An array of absolute start offsets, one per line.</returns>
    public static int[] BuildLineOffsets(string[] lines)
    {
        int[] offsets = new int[lines.Length];
        int current = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            offsets[i] = current;
            current += lines[i].Length + 1;
        }

        return offsets;
    }

    /// <summary>
    /// Determines whether an absolute character offset falls within any skip range.
    /// </summary>
    /// <param name="absoluteOffset">The offset to test.</param>
    /// <param name="ranges">The ranges to test against.</param>
    /// <returns><c>true</c> if the offset is inside a skip range, otherwise <c>false</c>.</returns>
    public static bool IsInSkipRange(int absoluteOffset, List<(int Start, int End)> ranges)
    {
        foreach ((int start, int end) in ranges)
        {
            if (absoluteOffset >= start && absoluteOffset < end)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines if a character is valid inside a Topsy Turvy identifier.
    /// </summary>
    /// <param name="character">The character to test.</param>
    /// <returns><c>true</c> if the character is a valid identifier character, otherwise <c>false</c>.</returns>
    public static bool IsIdentifierChar(char character) =>
        char.IsLetterOrDigit(character) || character == '-' || character == '_';

    /// <summary>
    /// Counts whole-word, case-insensitive occurrences of <paramref name="symbolName"/> across
    /// the source lines, excluding a specified line and any occurrences inside skip ranges.
    /// </summary>
    /// <param name="lines">The source lines.</param>
    /// <param name="lineOffsets">The absolute character offset at which each line begins.</param>
    /// <param name="skipRanges">The absolute offset ranges to exclude from scanning.</param>
    /// <param name="symbolName">The symbol name to count.</param>
    /// <param name="excludeLineIndex">The 0-indexed line to exclude from the count; pass <c>-1</c> to include all lines.</param>
    /// <returns>The number of occurrences found outside the excluded line and skip ranges.</returns>
    public static int CountOccurrences(
        string[] lines,
        int[] lineOffsets,
        List<(int Start, int End)> skipRanges,
        string symbolName,
        int excludeLineIndex)
    {
        int count = 0;
        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            if (lineIndex == excludeLineIndex)
            {
                continue;
            }

            string lineText = lines[lineIndex];
            int searchFrom = 0;
            int foundAt;

            while ((foundAt = lineText.IndexOf(symbolName, searchFrom, System.StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                searchFrom = foundAt + 1;
                if (foundAt > 0 && IsIdentifierChar(lineText[foundAt - 1]))
                {
                    continue;
                }

                int endChar = foundAt + symbolName.Length;
                if (endChar < lineText.Length && IsIdentifierChar(lineText[endChar]))
                {
                    continue;
                }

                int absoluteOffset = lineOffsets[lineIndex] + foundAt;
                if (IsInSkipRange(absoluteOffset, skipRanges))
                {
                    continue;
                }

                count++;
            }
        }

        return count;
    }
}
