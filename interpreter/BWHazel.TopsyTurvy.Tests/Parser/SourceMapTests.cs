using BWHazel.TopsyTurvy.Parser;

namespace BWHazel.TopsyTurvy.Tests.Parser;

/// <summary>
/// Tests for the <see cref="SourceMap"/> class.
/// </summary>
public class SourceMapTests
{
    /// <summary>
    /// Tests that the <see cref="SourceMap.GetOriginalLocation"/> method returns line 1, column 1 when the map has no entries.
    /// </summary>
    [Fact]
    public void GetOriginalLocation_WithEmptyMap_ReturnsLineOneColumnOne()
    {
        SourceMap sourceMap = new();

        (int line, int column) = sourceMap.GetOriginalLocation(0);

        Assert.Equal(1, line);
        Assert.Equal(1, column);
    }

    /// <summary>
    /// Tests that the <see cref="SourceMap.GetOriginalLocation"/> method returns line 1, column 1 when the offset precedes the first recorded mapping.
    /// </summary>
    [Fact]
    public void GetOriginalLocation_WithOffsetBeforeFirstMapping_ReturnsLineOneColumnOne()
    {
        SourceMap sourceMap = new();
        sourceMap.AddMapping(preProcessedOffset: 10, originalLine: 3, originalColumn: 1);

        (int line, int column) = sourceMap.GetOriginalLocation(0);

        Assert.Equal(1, line);
        Assert.Equal(1, column);
    }

    /// <summary>
    /// Tests that the <see cref="SourceMap.GetOriginalLocation"/> method returns the mapping base position when the offset exactly matches the first mapping boundary.
    /// </summary>
    [Fact]
    public void GetOriginalLocation_WithOffsetAtFirstMapping_ReturnsFirstMappingPosition()
    {
        SourceMap sourceMap = new();
        sourceMap.AddMapping(preProcessedOffset: 0, originalLine: 1, originalColumn: 1);

        (int line, int column) = sourceMap.GetOriginalLocation(0);

        Assert.Equal(1, line);
        Assert.Equal(1, column);
    }

    /// <summary>
    /// Tests that the <see cref="SourceMap.GetOriginalLocation"/> method returns the second mapping position when the offset exactly matches its boundary.
    /// </summary>
    [Fact]
    public void GetOriginalLocation_WithOffsetAtSecondMapping_ReturnsSecondMappingPosition()
    {
        SourceMap sourceMap = new();
        sourceMap.AddMapping(preProcessedOffset: 0, originalLine: 1, originalColumn: 1);
        sourceMap.AddMapping(preProcessedOffset: 10, originalLine: 2, originalColumn: 1);

        (int line, int column) = sourceMap.GetOriginalLocation(10);

        Assert.Equal(2, line);
        Assert.Equal(1, column);
    }

    /// <summary>
    /// Tests that the <see cref="SourceMap.GetOriginalLocation"/> method resolves an offset between two mappings to the earlier one, adding the remaining distance as a column offset.
    /// </summary>
    [Fact]
    public void GetOriginalLocation_WithOffsetBetweenMappings_ReturnsEarlierMappingWithColumnOffset()
    {
        SourceMap sourceMap = new();
        sourceMap.AddMapping(preProcessedOffset: 0, originalLine: 1, originalColumn: 1);
        sourceMap.AddMapping(preProcessedOffset: 10, originalLine: 2, originalColumn: 1);

        (int line, int column) = sourceMap.GetOriginalLocation(5);

        Assert.Equal(1, line);
        Assert.Equal(6, column);
    }

    /// <summary>
    /// Tests that the <see cref="SourceMap.GetOriginalLocation"/> method returns the last mapping position with the remaining distance added as a column offset when the offset exceeds all recorded mappings.
    /// </summary>
    [Fact]
    public void GetOriginalLocation_WithOffsetAfterLastMapping_ReturnsLastMappingWithColumnOffset()
    {
        SourceMap sourceMap = new();
        sourceMap.AddMapping(preProcessedOffset: 0, originalLine: 1, originalColumn: 1);
        sourceMap.AddMapping(preProcessedOffset: 10, originalLine: 2, originalColumn: 1);

        (int line, int column) = sourceMap.GetOriginalLocation(15);

        Assert.Equal(2, line);
        Assert.Equal(6, column);
    }

    /// <summary>
    /// Tests that the <see cref="SourceMap.GetOriginalLocation"/> method correctly selects the right mapping when many entries are present.
    /// </summary>
    [Fact]
    public void GetOriginalLocation_WithManyMappings_SelectsCorrectMapping()
    {
        SourceMap sourceMap = new();
        sourceMap.AddMapping(preProcessedOffset: 0, originalLine: 1, originalColumn: 1);
        sourceMap.AddMapping(preProcessedOffset: 10, originalLine: 2, originalColumn: 1);
        sourceMap.AddMapping(preProcessedOffset: 20, originalLine: 3, originalColumn: 1);
        sourceMap.AddMapping(preProcessedOffset: 30, originalLine: 4, originalColumn: 1);

        (int line, int column) = sourceMap.GetOriginalLocation(25);

        Assert.Equal(3, line);
        Assert.Equal(6, column);
    }

    /// <summary>
    /// Tests that the <see cref="SourceMap.GetOriginalLocation"/> method returns the mapping base column when the offset falls exactly at the start of a mapping segment.
    /// </summary>
    [Fact]
    public void GetOriginalLocation_WithOffsetAtMappingStart_ReturnsBaseColumn()
    {
        SourceMap sourceMap = new();
        sourceMap.AddMapping(preProcessedOffset: 0, originalLine: 1, originalColumn: 5);

        (int line, int column) = sourceMap.GetOriginalLocation(0);

        Assert.Equal(1, line);
        Assert.Equal(5, column);
    }

    /// <summary>
    /// Tests that the <see cref="SourceMap.AddMapping"/> method accumulates entries so that all added mappings are reflected in subsequent queries.
    /// </summary>
    [Fact]
    public void AddMapping_WhenCalledRepeatedly_AllMappingsAreReflected()
    {
        SourceMap map = new();
        map.AddMapping(preProcessedOffset: 0, originalLine: 1, originalColumn: 1);
        map.AddMapping(preProcessedOffset: 5, originalLine: 2, originalColumn: 1);

        (int line1, _) = map.GetOriginalLocation(0);
        (int line2, _) = map.GetOriginalLocation(5);

        Assert.Equal(1, line1);
        Assert.Equal(2, line2);
    }
}
