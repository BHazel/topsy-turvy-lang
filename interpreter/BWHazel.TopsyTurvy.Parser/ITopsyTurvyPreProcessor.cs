namespace BWHazel.TopsyTurvy.Parser;

/// <summary>
/// Defines methods for a source-text pre-processor that transforms the raw input before parsing.
/// </summary>
/// <remarks>
/// <para>
/// Pre-processors perform transformations on the source code prior to parsing, such as removing comments, and are executed as part
/// of a pipeline, <see cref="PreProcessorPipeline"/>.  Each pre-processor must implement <see cref="ITopsyTurvyPreProcessor"/> which
/// defines a single method, <see cref="Process"/>, that takes the current source text as returned from the previous pre-processor
/// in the pipeline or the original source code if executed first.  It also takes a <see cref="SourceMap"/> maintained by the pipeline
/// that tracks how positions in the transformed text correspond to positions in the original source code.  The pre-processor returns
/// its transformed text in a <see cref="PreProcessResult"/> and any source map updates are applied directly to the shared
/// <see cref="SourceMap"/> passed in.
/// </para>
/// </remarks>
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
