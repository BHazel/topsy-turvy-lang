using System;
using System.Collections.Generic;
using System.Linq;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Analysis;

/// <summary>
/// Scans Topsy Turvy source text into a flat list of categorised token spans for editor syntax highlighting.
/// </summary>
/// <remarks>
/// <para>
/// This is a lexical scanner, not a parser: it emits a best-effort category for every recognisable span even
/// when the surrounding source does not yet form valid syntax, for example while a user is still typing, and
/// it never throws. It is not a symbol-aware classifier and does not consult <see cref="SymbolTable"/>.
/// </para>
/// <para>
/// Keyword phrases are matched by longest-match against <see cref="KeywordData.Keywords"/> at every candidate
/// word-start position, rather than by re-splitting each phrase into individual words. Sorting candidates by
/// descending length resolves prefix collisions, for example <c>MY DUTY IS DISCHARGED.</c> versus
/// <c>MY DUTYIS PREMATURELY DISCHARGED.</c>.
/// </para>
/// </remarks>
public static class SourceTokeniser
{
    private const string BlockCommentOpen = "(ASIDE, AT SOME LENGTH:";
    private const string BlockCommentClose = "END OF ASIDE.)";
    private const string LineCommentOpen = "ASIDE:";

    private static readonly HashSet<string> TypeKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        Keywords.TypeNames.Peer,
        Keywords.TypeNames.Chancellor,
        Keywords.TypeNames.Pirate,
        Keywords.TypeNames.SausageRoll,
        Keywords.TypeNames.Fathom,
        Keywords.TypeNames.Foot,
        Keywords.TypeNames.Yarn,
        Keywords.TypeNames.Stitch,
        Keywords.TypeNames.Decree,
        "A LITTLE LIST OF",
    };

    private static readonly string[] KeywordsByDescendingLength =
        [.. KeywordData.Keywords
            .Select(entry => entry.Keyword)
            .OrderByDescending(keyword => keyword.Length)];

    /// <summary>
    /// Scans the source into a flat, document-ordered list of categorised token spans.
    /// </summary>
    /// <param name="source">The Topsy Turvy source text to scan.</param>
    /// <returns>The recognised tokens, in source order.</returns>
    public static IReadOnlyList<SourceToken> Tokenise(string source)
    {
        List<SourceToken> tokens = [];
        int[] lineStartOffsets = SourceAnalyser.BuildLineOffsets(source.Split('\n'));
        int length = source.Length;
        int position = 0;

        while (position < length)
        {
            char current = source[position];

            if (MatchesAt(source, position, BlockCommentOpen))
            {
                int closeIndex = source.IndexOf(BlockCommentClose, position, StringComparison.OrdinalIgnoreCase);
                int end = closeIndex < 0
                    ? length
                    : closeIndex + BlockCommentClose.Length;

                Add(tokens, lineStartOffsets, "comment", position, end);
                position = end;
                continue;
            }

            if (MatchesAt(source, position, LineCommentOpen))
            {
                int end = source.IndexOf('\n', position);
                if (end < 0)
                {
                    end = length;
                }

                Add(tokens, lineStartOffsets, "comment", position, end);
                position = end;
                continue;
            }

            if (current is '"' or '\'')
            {
                int end = ScanQuoted(source, position, current);
                Add(tokens, lineStartOffsets, "string", position, end);
                position = end;
                continue;
            }

            if (char.IsDigit(current) || (current == '-' && position + 1 < length && char.IsDigit(source[position + 1])))
            {
                int end = ScanNumber(source, position);
                Add(tokens, lineStartOffsets, "number", position, end);
                position = end;
                continue;
            }

            if (char.IsLetter(current) && !IsPrecededByIdentifierChar(source, position))
            {
                if (TryMatchPhrase(source, position, Keywords.SpecialNames.TheProps) is int specialEnd)
                {
                    Add(tokens, lineStartOffsets, "variable", position, specialEnd);
                    position = specialEnd;
                    continue;
                }

                if (TryMatchLongestKeyword(source, position) is (string keyword, int keywordEnd))
                {
                    string category = TypeKeywords.Contains(keyword)
                        ? "type"
                        : keyword.Equals(Keywords.TypeNames.Standing, StringComparison.OrdinalIgnoreCase)
                            ? "keywordOther"
                            : "keyword";
                    Add(tokens, lineStartOffsets, category, position, keywordEnd);
                    position = keywordEnd;
                    continue;
                }

                int identifierEnd = ScanIdentifier(source, position);
                Add(tokens, lineStartOffsets, "identifier", position, identifierEnd);
                position = identifierEnd;
                continue;
            }

            position++;
        }

        return tokens;
    }

    /// <summary>
    /// Determines whether the specified text occurs, case-insensitively, at the specified position.
    /// </summary>
    /// <param name="source">The source text.</param>
    /// <param name="position">The absolute offset to test.</param>
    /// <param name="text">The literal text to match.</param>
    /// <returns><c>true</c> if the specified text matches at the specified position, otherwise <c>false</c>.</returns>
    private static bool MatchesAt(string source, int position, string text) =>
        position + text.Length <= source.Length
            && string.Compare(source, position, text, 0, text.Length, StringComparison.OrdinalIgnoreCase) == 0;

    /// <summary>
    /// Determines whether the character immediately before the specified position is a valid identifier
    /// character, meaning the specified position is not the start of a new word.
    /// </summary>
    /// <param name="source">The source text.</param>
    /// <param name="position">The absolute offset to test.</param>
    /// <returns><c>true</c> if the specified position is preceded by an identifier character.</returns>
    private static bool IsPrecededByIdentifierChar(string source, int position) =>
        position > 0 && SourceAnalyser.IsIdentifierChar(source[position - 1]);

    /// <summary>
    /// Scans a single- or double-quoted literal, honouring the <c>~</c>-escape convention.
    /// </summary>
    /// <param name="source">The source text.</param>
    /// <param name="position">The absolute offset of the opening quote.</param>
    /// <param name="quote">The quote character, <c>"</c> or <c>'</c>.</param>
    /// <returns>The absolute offset one past the closing quote, or end-of-source if unterminated.</returns>
    private static int ScanQuoted(string source, int position, char quote)
    {
        int length = source.Length;
        int index = position + 1;
        while (index < length && source[index] != quote)
        {
            index += source[index] == '~' && index + 1 < length
                ? 2
                : 1;
        }

        return Math.Min(index + 1, length);
    }

    /// <summary>
    /// Scans an integer or floating-point numeric literal.
    /// </summary>
    /// <param name="source">The source text.</param>
    /// <param name="position">The absolute offset of the first character (a digit, or a leading <c>-</c>).</param>
    /// <returns>The absolute offset one past the final character of the literal.</returns>
    private static int ScanNumber(string source, int position)
    {
        int length = source.Length;
        int index = position;
        if (source[index] == '-')
        {
            index++;
        }

        while (index < length && char.IsDigit(source[index]))
        {
            index++;
        }

        if (index < length - 1 && source[index] == '.' && char.IsDigit(source[index + 1]))
        {
            index++;
            while (index < length && char.IsDigit(source[index]))
            {
                index++;
            }
        }

        return index;
    }

    /// <summary>
    /// Scans a plain identifier.
    /// </summary>
    /// <param name="source">The source text.</param>
    /// <param name="position">The absolute offset of the leading letter.</param>
    /// <remarks>
    /// A keyword phrase and special variable name match are attempted first.
    /// </remarks>
    /// <returns>The absolute offset one past the final character of the identifier.</returns>
    private static int ScanIdentifier(string source, int position)
    {
        int length = source.Length;
        int index = position + 1;
        while (index < length && SourceAnalyser.IsIdentifierChar(source[index]))
        {
            index++;
        }

        return index;
    }

    /// <summary>
    /// Attempts to match the longest entry in <see cref="KeywordData.Keywords"/> at the specified position.
    /// </summary>
    /// <param name="source">The source text.</param>
    /// <param name="position">The absolute offset to attempt a match from.</param>
    /// <returns>The matched keyword and its end offset, or <c>null</c> if none matched.</returns>
    private static (string Keyword, int End)? TryMatchLongestKeyword(string source, int position)
    {
        foreach (string keyword in KeywordsByDescendingLength)
        {
            if (TryMatchPhrase(source, position, keyword) is int end)
            {
                return (keyword, end);
            }
        }

        return null;
    }

    /// <summary>
    /// Attempts to match a possibly multi-word phrase at the specified position, allowing any run of
    /// whitespace between constituent words and requiring a non-identifier character, or end-of-source,
    /// immediately after the final word.
    /// </summary>
    /// <param name="source">The source text.</param>
    /// <param name="position">The absolute offset to attempt a match from.</param>
    /// <param name="phrase">The space-separated phrase to match, in its canonical case.</param>
    /// <returns>The absolute offset one past the final matched character, or <c>null</c> if no match.</returns>
    private static int? TryMatchPhrase(string source, int position, string phrase)
    {
        string[] words = phrase.Split(' ');
        int length = source.Length;
        int index = position;

        for (int wordIndex = 0; wordIndex < words.Length; wordIndex++)
        {
            if (wordIndex > 0)
            {
                int whitespaceStart = index;
                while (index < length && char.IsWhiteSpace(source[index]))
                {
                    index++;
                }

                if (index == whitespaceStart)
                {
                    return null;
                }
            }

            string word = words[wordIndex];
            if (index + word.Length > length || string.Compare(source, index, word, 0, word.Length, StringComparison.OrdinalIgnoreCase) != 0)
            {
                return null;
            }

            index += word.Length;
        }

        if (index < length && SourceAnalyser.IsIdentifierChar(source[index]))
        {
            return null;
        }

        return index;
    }

    /// <summary>
    /// Converts an absolute character offset and a category into a <see cref="SourceToken"/>, appending it to
    /// the provided token collection unless the span is empty.
    /// </summary>
    /// <param name="tokens">The token list to append to.</param>
    /// <param name="lineStartOffsets">The absolute start offset of each line, from <see cref="SourceAnalyser.BuildLineOffsets"/>.</param>
    /// <param name="category">The token category.</param>
    /// <param name="start">The absolute start offset, inclusive.</param>
    /// <param name="end">The absolute end offset, exclusive.</param>
    private static void Add(List<SourceToken> tokens, int[] lineStartOffsets, string category, int start, int end)
    {
        if (end <= start)
        {
            return;
        }

        SourceLocation startLocation = LocationAt(lineStartOffsets, start);
        SourceLocation endLocation = LocationAt(lineStartOffsets, end);
        tokens.Add(new(category, new(startLocation, endLocation)));
    }

    /// <summary>
    /// Converts an absolute character offset into a 1-indexed <see cref="SourceLocation"/>.
    /// </summary>
    /// <param name="lineStartOffsets">The absolute start offset of each line, from <see cref="SourceAnalyser.BuildLineOffsets"/>.</param>
    /// <param name="absoluteOffset">The absolute offset to convert.</param>
    /// <returns>The equivalent <see cref="SourceLocation"/>.</returns>
    private static SourceLocation LocationAt(int[] lineStartOffsets, int absoluteOffset)
    {
        int index = Array.BinarySearch(lineStartOffsets, absoluteOffset);
        int lineIndex = index >= 0
            ? index
            : ~index - 1;

        int column = absoluteOffset - lineStartOffsets[lineIndex] + 1;
        return new SourceLocation(lineIndex + 1, column);
    }
}
