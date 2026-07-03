using System;
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
    /// This adds ranges for block comments, string literals, and line comments in that priority order.
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
        int[] lineStartOffsets = new int[lines.Length];
        int current = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            lineStartOffsets[i] = current;
            current += lines[i].Length + 1;
        }

        return lineStartOffsets;
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
        char.IsLetterOrDigit(character) ||
            character == '-' ||
            character == '_';

    /// <summary>
    /// Finds the line and character positions of every specified whole-word match in the source.
    /// </summary>
    /// <param name="sourceLines">The source lines to scan.</param>
    /// <param name="word">The word to search for.</param>
    /// <remarks>
    /// The match is case-insensitive and returns all occurrences except in comments and string literals.  The search is performed in
    /// order from top to bottom, left to right.
    /// </remarks>
    /// <returns>
    /// A sequence of Line, Character pairs for every whole-word match in document order.
    /// </returns>
    public static IEnumerable<(int Line, int Character)> FindWordOccurrences(string[] sourceLines, string word)
    {
        // Source is joined into a single string as block comments can span multiples lines and line offsets are absolute
        // for the entire document.  Skip ranges are built using the absolute offsets.
        string joinedLines = string.Join("\n", sourceLines);
        int[] lineOffsets = BuildLineOffsets(sourceLines);
        List<(int Start, int End)> skipRanges = FindSkipRanges(joinedLines);

        for (int lineIndex = 0; lineIndex < sourceLines.Length; lineIndex++)
        {
            string lineText = sourceLines[lineIndex];
            int searchStartIndex = 0;
            int matchFoundIndex;

            while ((matchFoundIndex = lineText.IndexOf(word, searchStartIndex, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                searchStartIndex = matchFoundIndex + 1;

                // Check the character before the found match to ensure it is not part of a larger identifier.
                if (matchFoundIndex > 0 && IsIdentifierChar(lineText[matchFoundIndex - 1]))
                {
                    continue;
                }

                // Check the character after the found match to ensure it is not part of a larger identifier.
                int endCharacter = matchFoundIndex + word.Length;
                if (endCharacter < lineText.Length && IsIdentifierChar(lineText[endCharacter]))
                {
                    continue;
                }

                // Check the absolute offset of the found match to ensure it is not inside a skip range.
                int absoluteOffset = lineOffsets[lineIndex] + matchFoundIndex;
                if (IsInSkipRange(absoluteOffset, skipRanges))
                {
                    continue;
                }

                yield return (lineIndex, matchFoundIndex);
            }
        }
    }

    /// <summary>
    /// Counts the number of occurrences of a specified whole-word symbol in the source lines excluding a specified line.
    /// </summary>
    /// <param name="sourceLines">The source lines.</param>
    /// <param name="symbolName">The symbol name to count.</param>
    /// <param name="excludeLineIndex">The 0-indexed line to exclude from the count; pass <c>-1</c> to include all lines.</param>
    /// <remarks>
    /// The search is performed in order from top to bottom, left to right.  The count excludes any occurrences inside comments or
    /// string literals.
    /// </remarks>
    /// <returns>The number of occurrences found outside the excluded line and skip ranges.</returns>
    public static int CountOccurrences(string[] sourceLines, string symbolName, int excludeLineIndex)
    {
        int count = 0;
        foreach ((int lineIndex, int _) in FindWordOccurrences(sourceLines, symbolName))
        {
            if (lineIndex != excludeLineIndex)
            {
                count++;
            }
        }

        return count;
    }
}
