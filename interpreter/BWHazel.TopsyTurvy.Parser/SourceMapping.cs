namespace BWHazel.TopsyTurvy.Parser;

/// <summary>
/// Mapping of offsets in transformed text back to positions in the original source file.
/// </summary>
/// <param name="TransformedOffset">The offset in the transformed text.</param>
/// <param name="OriginalLine">The line number in the original source file.</param>
/// <param name="OriginalColumn">The column number in the original source file.</param>
/// <remarks>
/// This stores a mapping of the overall position in the transformed text mapped to the corresponding line and column in the original
/// source, intended to be stored in a <see cref="SourceMap"/>.  For an overall offset of 42 characters in the transformed text, mapped
/// to an original source position of line 3, column 1, the mapping would be:
/// <code>
/// new SourceMapping(42, 3, 1);
/// </code>
/// </remarks>
public record SourceMapping(int TransformedOffset, int OriginalLine, int OriginalColumn);