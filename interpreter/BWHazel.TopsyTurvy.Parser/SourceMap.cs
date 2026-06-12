using System.Collections.Generic;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Parser;

/// <summary>
/// Maps offsets in transformed text back to positions in the original source file.
/// </summary>
/// <remarks>
/// <para>
/// The source map contains <see cref="SourceMapping"/>s that are set during pre-processing and enable tooling to report errors and
/// diagnostics to original source locations rather than in the transformed text.  As an example, the <c>~</c> "Victorian Flourish"
/// character is used as a line continuation character to allow the user to split a line across several but is removed and the lines
/// joined together during transformation.
/// </para>
/// <para>
/// A source map is built incrementally by each pre-processor in the pipeline and passed onto the next.  Each pre-processor is
/// responsible for adding mappings for any transformations it performs to the source map, although it is not always essential to do
/// so.  As an example, the <see cref="CommentsPreProcessor"/> preserves all newline characters when removing the comment text,
/// therefore line counts remain the same and no additional source mappings are required.
/// </para>
/// <para>
/// Each source map contains a list of <see cref="SourceMapping"/>s that represent the mappings from transformed text to original source
/// locations.  In practice, all mappings record the start of a line in the original source, therefore
/// <see cref="SourceMapping.OriginalColumn"/> is always 1.  As an example:
/// <code>
/// Offset 0 => Line 1, Column 1
/// Offset 24 => Line 3, Column 1
/// Offset 51 => Line 5, Column 1
/// </code>
/// contains 3 source mappings.  The offset corresponds to the overall character position in the transformed text, not its line or
/// column position.  This offset is mapped to the line and column in the original source file where that character came from.
/// </para>
/// <para>
/// ### Finding Original Locations
/// To find the original source location for a given offset in the transformed text, a binary search is performed on the list of source
/// mappings in the source map:
/// * The search bounds are set to the start (lower bound) and end (upper bound) of the source mappings list.
/// * Iteratively, while the lower bound is less than or equal to the upper bound:
///     * The midpoint index is calculated as the average of the lower and upper bounds.
///     * If the transformed offset at the midpoint is less than or equal to the target offset, the lower bound is moved to the midpoint plus one.
///     * Otherwise, the upper bound is moved to the midpoint minus one.
/// * After the loop, the upper bound will be at the index of the nearest preceding source mapping.
///     * If the upper bound is negative, it means there are no mappings before the target offset, and it defaults to line 1, column 1 of the original source.
/// * The original source mapping information can be retrieved from that nearest source mapping:
///     * Original line is the mapping line.
///     * Original column is the mapping column plus the number of characters between the start of the line (the mapping offset) and the target character offset, i.e. how far into the line the target is.
/// </para>
/// </remarks>
public class SourceMap
{
    private readonly List<SourceMapping> mappings = [];

    /// <summary>
    /// Records a mapping between a transformed position and its original source position.
    /// </summary>
    /// <param name="transformedOffset">Zero-based character offset in the transformed text.</param>
    /// <param name="originalLine">1-indexed line number in the original source.</param>
    /// <param name="originalColumn">1-indexed column number in the original source.</param>
    public void AddMapping(int transformedOffset, int originalLine, int originalColumn) =>
        this.mappings.Add(new(transformedOffset, originalLine, originalColumn));

    /// <summary>
    /// Translates a transformed character offset back to original source coordinates.
    /// </summary>
    /// <remarks>
    /// This method uses a binary search to find the nearest preceding mapping and calculates the original position
    /// by applying the offset from that mapping.  If no mappings are found before the given offset, it defaults to
    /// line 1, column 1 of the original source.
    /// </remarks>
    /// <param name="transformedOffset">Zero-based character offset in the transformed text.</param>
    /// <returns>The 1-indexed line and column in the original source.</returns>
    public SourceLocation GetOriginalLocation(int transformedOffset)
    {
        int lowerBound = 0;
        int upperBound = this.mappings.Count - 1;

        while (lowerBound <= upperBound)
        {
            int midpointIndex = (lowerBound + upperBound) / 2;
            if (this.mappings[midpointIndex].TransformedOffset <= transformedOffset)
            {
                lowerBound = midpointIndex + 1;
            }
            else
            {
                upperBound = midpointIndex - 1;
            }
        }

        if (upperBound < 0)
        {
            return new(1, 1);
        }

        SourceMapping nearestLineMapping = this.mappings[upperBound];
        int columnOffsetFromLineStart = transformedOffset - nearestLineMapping.TransformedOffset;
        return new(nearestLineMapping.OriginalLine, nearestLineMapping.OriginalColumn + columnOffsetFromLineStart);
    }
}
