using System.Collections.Generic;
using System.Linq;
using BWHazel.TopsyTurvy.Analysis;

namespace BWHazel.TopsyTurvy.Tests.Analysis;

/// <summary>
/// Tests for <see cref="SourceAnalyser"/>.
/// </summary>
public class SourceAnalyserTests
{
    /// <summary>
    /// Tests that the <see cref="SourceAnalyser.IsIdentifierChar"/> method correctly identifies letters as valid identifier characters.
    /// </summary>
    [Theory]
    [InlineData('a')]
    [InlineData('Z')]
    [InlineData('m')]
    public void IsIdentifierChar_WithLetter_ReturnsTrue(char character)
    {
        Assert.True(SourceAnalyser.IsIdentifierChar(character));
    }

    /// <summary>
    /// Tests that the <see cref="SourceAnalyser.IsIdentifierChar"/> method correctly identifies digits as valid identifier characters.
    /// </summary>
    [Theory]
    [InlineData('0')]
    [InlineData('5')]
    [InlineData('9')]
    public void IsIdentifierChar_WithDigit_ReturnsTrue(char character)
    {
        Assert.True(SourceAnalyser.IsIdentifierChar(character));
    }

    /// <summary>
    /// Tests that the <see cref="SourceAnalyser.IsIdentifierChar"/> method correctly identifies a hyphen as a valid identifier character.
    /// </summary>
    [Fact]
    public void IsIdentifierChar_WithHyphen_ReturnsTrue()
    {
        Assert.True(SourceAnalyser.IsIdentifierChar('-'));
    }

    /// <summary>
    /// Tests that the <see cref="SourceAnalyser.IsIdentifierChar"/> method correctly identifies an underscore as a valid identifier character.
    /// </summary>
    [Fact]
    public void IsIdentifierChar_WithUnderscore_ReturnsTrue()
    {
        Assert.True(SourceAnalyser.IsIdentifierChar('_'));
    }

    /// <summary>
    /// Tests that the <see cref="SourceAnalyser.IsIdentifierChar"/> method correctly identifies various non-identifier characters as invalid.
    /// </summary>
    [Theory]
    [InlineData(' ')]
    [InlineData('.')]
    [InlineData('!')]
    [InlineData(',')]
    [InlineData('"')]
    public void IsIdentifierChar_WithPunctuationCharacter_ReturnsFalse(char character)
    {
        Assert.False(SourceAnalyser.IsIdentifierChar(character));
    }

    /// <summary>
    /// Tests that the <see cref="SourceAnalyser.BuildLineOffsets"/> method verifies that a single line starts at offset zero.
    /// </summary>
    [Fact]
    public void BuildLineOffsets_WithSingleLine_ReturnsZeroOffset()
    {
        int[] offsets = SourceAnalyser.BuildLineOffsets(["hello"]);

        Assert.Single(offsets);
        Assert.Equal(0, offsets[0]);
    }

    /// <summary>
    /// Tests that the <see cref="SourceAnalyser.BuildLineOffsets"/> method correctly calculates offsets for multiple lines, ensuring that each line's offset accounts for the length of the previous line plus a new-line character.
    /// </summary>
    [Fact]
    public void BuildLineOffsets_WithMultipleLines_ReturnsCorrectOffsets()
    {
        int[] offsets = SourceAnalyser.BuildLineOffsets(["hello", "world", "foo"]);

        Assert.Equal(3, offsets.Length);
        Assert.Equal(0, offsets[0]);
        Assert.Equal(6, offsets[1]);
        Assert.Equal(12, offsets[2]);
    }

    /// <summary>
    /// Tests that the <see cref="SourceAnalyser.BuildLineOffsets"/> method returns an empty array when given an empty input array.
    /// </summary>
    [Fact]
    public void BuildLineOffsets_WithEmptyArray_ReturnsEmptyArray()
    {
        int[] offsets = SourceAnalyser.BuildLineOffsets([]);
        Assert.Empty(offsets);
    }

    /// <summary>
    /// Tests that the <see cref="SourceAnalyser.BuildLineOffsets"/> method produces no skip ranges when the source contains no comments or string literals.
    /// </summary>
    [Fact]
    public void FindSkipRanges_WithPlainSource_ReturnsEmptySkipRanges()
    {
        List<(int Start, int End)> skipRanges = SourceAnalyser.FindSkipRanges("BEHOLD x");
        Assert.Empty(skipRanges);
    }

    /// <summary>
    /// Tests that the <see cref="SourceAnalyser.FindSkipRanges"/> method produces a skip range for a string literal.
    /// </summary>
    [Fact]
    public void FindSkipRanges_WithStringLiteral_ReturnsRangeCoveringLiteral()
    {
        List<(int Start, int End)> skipRanges = SourceAnalyser.FindSkipRanges("BEHOLD \"hello\"");

        Assert.Single(skipRanges);
        Assert.Equal(7, skipRanges[0].Start);
        Assert.Equal(14, skipRanges[0].End);
    }

    /// <summary>
    /// Tests that the <see cref="SourceAnalyser.FindSkipRanges"/> method produces a skip range for a line comment.
    /// </summary>
    [Fact]
    public void FindSkipRanges_WithLineComment_ReturnsRangeToEndOfSource()
    {
        string source = "BEHOLD x ASIDE: comment";

        List<(int Start, int End)> skipRanges = SourceAnalyser.FindSkipRanges(source);
        Assert.Single(skipRanges);
        Assert.Equal(9, skipRanges[0].Start);
        Assert.Equal(source.Length, skipRanges[0].End);
    }

    /// <summary>
    /// Tests that the <see cref="SourceAnalyser.FindSkipRanges"/> method produces a single skip range for an entire block comment.
    /// </summary>
    [Fact]
    public void FindSkipRanges_WithBlockComment_ReturnsSingleRange()
    {
        string source = "(ASIDE, AT SOME LENGTH: text END OF ASIDE.)";

        List<(int Start, int End)> skipRanges = SourceAnalyser.FindSkipRanges(source);
        Assert.Single(skipRanges);
        Assert.Equal(0, skipRanges[0].Start);
        Assert.Equal(source.Length, skipRanges[0].End);
    }

    /// <summary>
    /// Tests that the <see cref="SourceAnalyser.FindSkipRanges"/> method does not produce separate skip ranges for string literals that appear inside block comments.
    /// </summary>
    [Fact]
    public void FindSkipRanges_WithStringInsideBlockComment_NotAddedAsSeparateRange()
    {
        string source = "(ASIDE, AT SOME LENGTH: \"quoted\" END OF ASIDE.)";

        List<(int Start, int End)> skipRanges = SourceAnalyser.FindSkipRanges(source);
        Assert.Single(skipRanges);
    }

    /// <summary>
    /// Tests that the <see cref="SourceAnalyser.FindSkipRanges"/> method does not produce separate skip ranges for line comments that appear inside string literals.
    /// </summary>
    [Fact]
    public void FindSkipRanges_WithLineCommentInsideString_NotAddedAsSeparateRange()
    {
        string source = "BEHOLD \"ASIDE: not a comment\"";

        List<(int Start, int End)> skipRanges = SourceAnalyser.FindSkipRanges(source);
        Assert.Single(skipRanges);
        Assert.Equal(7, skipRanges[0].Start);
    }

    /// <summary>
    /// Tests that the <see cref="SourceAnalyser.IsInSkipRange"/> method reports an offset inside a range as skipped.
    /// </summary>
    [Fact]
    public void IsInSkipRange_WithOffsetInsideRange_ReturnsTrue()
    {
        List<(int Start, int End)> skipRanges = [(5, 15)];

        Assert.True(SourceAnalyser.IsInSkipRange(10, skipRanges));
    }

    /// <summary>
    /// Tests that the <see cref="SourceAnalyser.IsInSkipRange"/> method reports the start of a range as included.
    /// </summary>
    [Fact]
    public void IsInSkipRange_WithOffsetAtRangeStart_ReturnsTrue()
    {
        List<(int Start, int End)> skipRanges = [(5, 15)];

        Assert.True(SourceAnalyser.IsInSkipRange(5, skipRanges));
    }

    /// <summary>
    /// Tests that the <see cref="SourceAnalyser.IsInSkipRange"/> method reports an offset at the end of a range as excluded.
    /// </summary>
    [Fact]
    public void IsInSkipRange_WithOffsetAtRangeEnd_ReturnsFalse()
    {
        List<(int Start, int End)> skipRanges = [(5, 15)];

        Assert.False(SourceAnalyser.IsInSkipRange(15, skipRanges));
    }

    /// <summary>
    /// Tests that the <see cref="SourceAnalyser.IsInSkipRange"/> method reports an offset before a range as not skipped.
    /// </summary>
    [Fact]
    public void IsInSkipRange_WithOffsetBeforeRange_ReturnsFalse()
    {
        List<(int Start, int End)> skipRanges = [(5, 15)];

        Assert.False(SourceAnalyser.IsInSkipRange(4, skipRanges));
    }

    /// <summary>
    /// Tests that the <see cref="SourceAnalyser.IsInSkipRange"/> returns <c>false</c> for an empty range list.
    /// </summary>
    [Fact]
    public void IsInSkipRange_WithEmptyRangeList_ReturnsFalse()
    {
        Assert.False(SourceAnalyser.IsInSkipRange(0, []));
    }

    /// <summary>
    /// Tests that the <see cref="SourceAnalyser.CountOccurrences"/> method correctly counts multiple occurrences of a word across different lines.
    /// </summary>
    [Fact]
    public void CountOccurrences_WithMultipleMatches_ReturnsCorrectCount()
    {
        string source = "greet alpha\nSUMMON greet WITH NOTHING IF YOU PLEASE.\ngreet";
        Assert.Equal(3, CountInSource(source, "greet"));
    }

    /// <summary>
    /// Tests that the <see cref="SourceAnalyser.CountOccurrences"/> method excludes occurrences of the specified word on the specified line.
    /// </summary>
    [Fact]
    public void CountOccurrences_WithMultipleOccurrencesAndExcludesSpecifiedLine_ReturnsCorrectCount()
    {
        string source = "greet greet\nSUMMON greet WITH NOTHING IF YOU PLEASE.\ngreet";

        Assert.Equal(2, CountInSource(source, "greet", excludeLineIndex: 0));
    }

    /// <summary>
    /// Tests that the <see cref="SourceAnalyser.CountOccurrences"/> method includes all lines when given an exclusion index of -1.
    /// </summary>
    [Fact]
    public void CountOccurrences_WithExcludeMinusOne_CountsAllLines()
    {
        string source = "greet\ngreet\ngreet";

        Assert.Equal(3, CountInSource(source, "greet", excludeLineIndex: -1));
    }

    /// <summary>
    /// Tests that the <see cref="SourceAnalyser.CountOccurrences"/> method does not include partial matches of words.
    /// </summary>
    [Fact]
    public void CountOccurrences_WithPartialWordMatch_NotCounted()
    {
        string source = "greeting greet";

        Assert.Equal(1, CountInSource(source, "greet"));
    }

    /// <summary>
    /// Tests that the <see cref="SourceAnalyser.CountOccurrences"/> method is case-insensitive.
    /// </summary>
    [Fact]
    public void CountOccurrences_WithCaseInsensitiveWords_MatchesAllCasings()
    {
        string source = "greet Greet GREET";
        Assert.Equal(3, CountInSource(source, "greet"));
    }

    /// <summary>
    /// Tests that the <see cref="SourceAnalyser.CountOccurrences"/> method does not count occurrences of the word that appear inside string literals.
    /// </summary>
    [Fact]
    public void CountOccurrences_WithOccurrenceInsideStringLiteral_NotCounted()
    {
        string source = "BEHOLD \"greet world\"";

        Assert.Equal(0, CountInSource(source, "greet"));
    }

    /// <summary>
    /// Tests that the <see cref="SourceAnalyser.CountOccurrences"/> method does not count occurrences of the word that appear inside line comments.
    /// </summary>
    [Fact]
    public void CountOccurrences_WithOccurrenceInsideLineComment_NotCounted()
    {
        string source = "BEHOLD \"hello\" ASIDE: greet";

        Assert.Equal(0, CountInSource(source, "greet"));
    }

    /// <summary>
    /// Tests that the <see cref="SourceAnalyser.CountOccurrences"/> method does not count occurrences of the word that appear inside block comments.
    /// </summary>
    [Fact]
    public void CountOccurrences_WithOccurrenceInsideBlockComment_NotCounted()
    {
        string source = "(ASIDE, AT SOME LENGTH: greet greet END OF ASIDE.)";

        Assert.Equal(0, CountInSource(source, "greet"));
    }

    /// <summary>
    /// Tests that the <see cref="SourceAnalyser.CountOccurrences"/> method returns zero when the specified word does not appear.
    /// </summary>
    [Fact]
    public void CountOccurrences_WithWordNotPresent_ReturnsZero()
    {
        string source = "BEHOLD \"hello\"";

        Assert.Equal(0, CountInSource(source, "greet"));
    }


    /// <summary>
    /// Tests that <see cref="SourceAnalyser.FindWordOccurrences"/> returns the correct line and character position for a single match.
    /// </summary>
    [Fact]
    public void FindWordOccurrences_WithSingleMatch_ReturnsCorrectPosition()
    {
        string[] lines = ["BEHOLD greet"];

        IEnumerable<(int Line, int Character)> result = SourceAnalyser.FindWordOccurrences(lines, "greet");

        (int Line, int Character) = Assert.Single(result);
        Assert.Equal(0, Line);
        Assert.Equal(7, Character);
    }

    /// <summary>
    /// Tests that <see cref="SourceAnalyser.FindWordOccurrences"/> returns matches across multiple lines.
    /// </summary>
    [Fact]
    public void FindWordOccurrences_WithMatchesOnMultipleLines_ReturnsAllPositions()
    {
        string[] lines = ["greet alpha", "SUMMON greet WITH NOTHING IF YOU PLEASE.", "greet"];

        IEnumerable<(int Line, int Character)> result = SourceAnalyser.FindWordOccurrences(lines, "greet");

        Assert.Equal(3, Enumerable.Count(result));
    }

    /// <summary>
    /// Tests that <see cref="SourceAnalyser.FindWordOccurrences"/> does not return partial word matches.
    /// </summary>
    [Fact]
    public void FindWordOccurrences_WithPartialWordMatch_NotReturned()
    {
        string[] lines = ["greeting greet"];

        IEnumerable<(int Line, int Character)> result = SourceAnalyser.FindWordOccurrences(lines, "greet");

        Assert.Single(result);
    }

    /// <summary>
    /// Tests that <see cref="SourceAnalyser.FindWordOccurrences"/> is case-insensitive.
    /// </summary>
    [Fact]
    public void FindWordOccurrences_WithDifferentCasings_MatchesAll()
    {
        string[] lines = ["greet Greet GREET"];

        IEnumerable<(int Line, int Character)> result = SourceAnalyser.FindWordOccurrences(lines, "greet");

        Assert.Equal(3, Enumerable.Count(result));
    }

    /// <summary>
    /// Tests that <see cref="SourceAnalyser.FindWordOccurrences"/> does not return occurrences inside string literals.
    /// </summary>
    [Fact]
    public void FindWordOccurrences_WithOccurrenceInsideStringLiteral_NotReturned()
    {
        string[] lines = [@"BEHOLD ""greet world"""];

        IEnumerable<(int Line, int Character)> result = SourceAnalyser.FindWordOccurrences(lines, "greet");

        Assert.Empty(result);
    }

    /// <summary>
    /// Tests that <see cref="SourceAnalyser.FindWordOccurrences"/> does not return occurrences inside line comments.
    /// </summary>
    [Fact]
    public void FindWordOccurrences_WithOccurrenceInsideLineComment_NotReturned()
    {
        string[] lines = [@"BEHOLD ""hello"" ASIDE: greet"];

        IEnumerable<(int Line, int Character)> result = SourceAnalyser.FindWordOccurrences(lines, "greet");

        Assert.Empty(result);
    }

    /// <summary>
    /// Tests that <see cref="SourceAnalyser.FindWordOccurrences"/> does not return occurrences inside block comments spanning multiple lines.
    /// </summary>
    [Fact]
    public void FindWordOccurrences_WithOccurrenceInsideBlockComment_NotReturned()
    {
        string[] lines = ["(ASIDE, AT SOME LENGTH: greet greet END OF ASIDE.)"];

        IEnumerable<(int Line, int Character)> result = SourceAnalyser.FindWordOccurrences(lines, "greet");

        Assert.Empty(result);
    }

    /// <summary>
    /// Tests that <see cref="SourceAnalyser.FindWordOccurrences"/> returns an empty sequence when no matches exist.
    /// </summary>
    [Fact]
    public void FindWordOccurrences_WithNoMatches_ReturnsEmpty()
    {
        string[] lines = [@"BEHOLD ""hello"""];

        IEnumerable<(int Line, int Character)> result = SourceAnalyser.FindWordOccurrences(lines, "greet");

        Assert.Empty(result);
    }

    /// <summary>
    /// Helper method to count occurrences of a word in a source string, excluding occurrences on a specified line index.
    /// </summary>
    /// <param name="source">The source string to search.</param>
    /// <param name="word">The word to count occurrences of.</param>
    /// <param name="excludeLineIndex">The index of the line to exclude from counting.</param>
    /// <remarks>
    /// This method builds the necessary line offsets and skip ranges before calling the main counting method.
    /// </remarks>
    /// <returns>The number of occurrences of the word in the source string, excluding the specified line.</returns>
    private static int CountInSource(string source, string word, int excludeLineIndex = -1)
    {
        string[] lines = source.Split('\n');
        return SourceAnalyser.CountOccurrences(lines, word, excludeLineIndex);
    }
}
