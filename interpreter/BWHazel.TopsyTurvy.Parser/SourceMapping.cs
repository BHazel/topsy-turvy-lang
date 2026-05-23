namespace BWHazel.TopsyTurvy.Parser;

/// <summary>
/// Mapping of offsets in pre-processed text back to positions in the original source file.
/// </summary>
/// <param name="PreProcessedOffset">The offset in the pre-processed text.</param>
/// <param name="OriginalLine">The line number in the original source file.</param>
/// <param name="OriginalColumn">The column number in the original source file.</param>
public record SourceMapping(int PreProcessedOffset, int OriginalLine, int OriginalColumn);