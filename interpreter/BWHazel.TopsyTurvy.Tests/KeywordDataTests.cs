using BWHazel.TopsyTurvy.Analysis;

namespace BWHazel.TopsyTurvy.Tests;

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
        Assert.All(KeywordData.Keywords, entry => Assert.False(string.IsNullOrWhiteSpace(entry.Keyword)));
    }

    /// <summary>
    /// Tests that the <see cref="KeywordData.Keywords"/> array contains no entries with empty or whitespace-only detail descriptions.
    /// </summary>
    [Fact]
    public void Keywords_AllEntriesHaveNonEmptyDetail()
    {
        Assert.All(KeywordData.Keywords, entry => Assert.False(string.IsNullOrWhiteSpace(entry.Detail)));
    }
}
