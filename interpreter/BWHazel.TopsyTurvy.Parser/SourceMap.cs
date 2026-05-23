using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Parser;

/// <summary>
/// Maps offsets in pre-processed text back to positions in the original source file.
/// </summary>
public class SourceMap
{
    private readonly List<SourceMapping> mappings = [];

    /// <summary>
    /// Records a mapping between a pre-processed position and its original source position.
    /// </summary>
    /// <param name="preProcessedOffset">Zero-based character offset in the pre-processed text.</param>
    /// <param name="originalLine">1-indexed line number in the original source.</param>
    /// <param name="originalColumn">1-indexed column number in the original source.</param>
    public void AddMapping(int preProcessedOffset, int originalLine, int originalColumn) =>
        this.mappings.Add(new(preProcessedOffset, originalLine, originalColumn));

    /// <summary>
    /// Translates a pre-processed character offset back to original source coordinates.
    /// </summary>
    /// <param name="offset">Zero-based character offset in the pre-processed text.</param>
    /// <returns>The 1-indexed line and column in the original source.</returns>
    public (int Line, int Column) GetOriginalLocation(int offset)
    {
        int low = 0;
        int high = this.mappings.Count - 1;

        while (low <= high)
        {
            int mid = (low + high) / 2;
            if (this.mappings[mid].PreProcessedOffset <= offset)
            {
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        if (high < 0)
        {
            return (1, 1);
        }

        SourceMapping sourceMapping = this.mappings[high];
        int columnOffset = offset - sourceMapping.PreProcessedOffset;
        return (sourceMapping.OriginalLine, sourceMapping.OriginalColumn + columnOffset);
    }
}
