using BWHazel.TopsyTurvy.Parser;

namespace BWHazel.TopsyTurvy.Tests.Parser;

/// <summary>
/// Adds a single mapping to the source map and returns the input text unchanged.
/// </summary>
/// <param name="offset">The pre-processed character offset to record.</param>
/// <param name="line">The 1-indexed original source line to record.</param>
/// <param name="column">The 1-indexed original source column to record.</param>
/// <remarks>
/// This pre-processor is intended for testing purposes only.
/// </remarks>
internal sealed class TestMappingAddingPreProcessor(int offset, int line, int column) : ITopsyTurvyPreProcessor
{
    /// <summary>
    /// Adds the configured mapping to the provided <see cref="SourceMap"/> and returns the input text unchanged.
    /// </summary>
    /// <param name="input">The source text to pass through.</param>
    /// <param name="currentSourceMap">The source map to extend with the configured mapping.</param>
    /// <returns>A <see cref="PreProcessResult"/> with the input text and the updated source map.</returns>
    public PreProcessResult Process(string input, SourceMap currentSourceMap)
    {
        currentSourceMap.AddMapping(offset, line, column);
        return new(input, currentSourceMap);
    }
}
