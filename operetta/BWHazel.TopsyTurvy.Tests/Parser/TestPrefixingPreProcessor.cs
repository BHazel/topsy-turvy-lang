using BWHazel.TopsyTurvy.Parser;

namespace BWHazel.TopsyTurvy.Tests.Parser;

/// <summary>
/// Prepends a fixed prefix to the input text.
/// </summary>
/// <param name="prefix">The text to prepend to the input.</param>
/// <remarks>
/// This pre-processor is intended for testing purposes only.
/// </remarks>
internal sealed class TestPrefixingPreProcessor(string prefix) : ITopsyTurvyPreProcessor
{
    /// <summary>
    /// Prepends a specified prefix to the input text and returns the source map unchanged.
    /// </summary>
    /// <param name="input">The source text to transform.</param>
    /// <param name="currentSourceMap">The source map to pass through unchanged.</param>
    /// <returns>A <see cref="PreProcessResult"/> with the prefix prepended to the text.</returns>
    public PreProcessResult Process(string input, SourceMap currentSourceMap) =>
        new(prefix + input, currentSourceMap);
}
