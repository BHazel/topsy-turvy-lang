namespace BWHazel.TopsyTurvy.Parser;

/// <summary>
/// Defines methods for a source-text pre-processor that transforms the raw input before parsing.
/// </summary>
public interface ITopsyTurvyPreProcessor
{
    /// <summary>
    /// Applies a transformation to the supplied source text.
    /// </summary>
    /// <param name="input">The source text to process.</param>
    /// <param name="currentSourceMap">The source map built by preceding processors.</param>
    /// <returns>A <see cref="PreProcessResult"/> containing the transformed text and source map.</returns>
    PreProcessResult Process(string input, SourceMap currentSourceMap);
}
