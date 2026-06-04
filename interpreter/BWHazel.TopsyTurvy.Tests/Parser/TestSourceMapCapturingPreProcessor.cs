using BWHazel.TopsyTurvy.Parser;

namespace BWHazel.TopsyTurvy.Tests.Parser;

/// <summary>
/// Captures the <see cref="SourceMap"/> instance passed to it.
/// </summary>
/// <remarks>
/// This pre-processor is intended for testing purposes only.
/// </remarks>
internal sealed class TestSourceMapCapturingPreProcessor : ITopsyTurvyPreProcessor
{
    /// <summary>
    /// Gets the <see cref="SourceMap"/> instance most recently passed to <see cref="Process"/>.
    /// </summary>
    public SourceMap? CapturedMap { get; private set; }

    /// <summary>
    /// Captures the source map instance and returns the input text unchanged.
    /// </summary>
    /// <param name="input">The source text to pass through.</param>
    /// <param name="currentSourceMap">The source map instance to capture.</param>
    /// <returns>A <see cref="PreProcessResult"/> with the input text and source map unchanged.</returns>
    public PreProcessResult Process(string input, SourceMap currentSourceMap)
    {
        this.CapturedMap = currentSourceMap;
        return new(input, currentSourceMap);
    }
}
