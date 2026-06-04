using BWHazel.TopsyTurvy.Analysis;

namespace BWHazel.TopsyTurvy.Tests.Analysis;

/// <summary>
/// Tests for the <see cref="KeywordData"/> class.
/// </summary>
public class KeywordDataTests
{
    /// <summary>
    /// Tests that the <see cref="KeywordData.Keywords"/> array contains no keywords with empty or whitespace-only strings.
    /// </summary>
    [Fact]
    public void Keywords_AllEntriesHaveNonEmptyKeyword()
    {
        KeywordData.Keywords.ShouldAllBe(entry => !string.IsNullOrWhiteSpace(entry.Keyword));
    }

    /// <summary>
    /// Tests that the <see cref="KeywordData.Keywords"/> array contains no entries with empty or whitespace-only detail descriptions.
    /// </summary>
    [Fact]
    public void Keywords_AllEntriesHaveNonEmptyDetail()
    {
        KeywordData.Keywords.ShouldAllBe(entry => !string.IsNullOrWhiteSpace(entry.Detail));
    }
}
