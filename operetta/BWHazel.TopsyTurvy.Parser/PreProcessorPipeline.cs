using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Parser;

/// <summary>
/// Runs a sequential chain of <see cref="ITopsyTurvyPreProcessor"/> instances over source text.
/// </summary>
/// <remarks>
/// <para>
/// The pre-processor pipeline applies a series of transformations to the original source code prior to parsing.  Each
/// pre-processor in the pipeline implements the <see cref="ITopsyTurvyPreProcessor"/> interface and performs a specific
/// transformation, such as removing comments (<see cref="CommentsPreProcessor"/>).  Each pre-processor receives the output of the
/// previous one, with the first in the pipeline receiving the original source code.  The output of each pre-processor is stored in
/// a <see cref="PreProcessResult"/> which contains the transformed text and a <see cref="SourceMap"/> that tracks any position
/// offsets between the transformed text and the original source.
/// </para>
/// <para>
/// During execution, each pre-processor is applied in order of addition to the pipeline.  A single <see cref="SourceMap"/> is used
/// throughout the entire pipeline, passed to each pre-processor and updated in-place.  The resulting <see cref="PreProcessResult"/>
/// therefore contains the final transformed text and complete source map.
/// </para>
/// <para>
/// ### Example Usage
/// <code>
/// PreProcessorPipeline preProcessorPipeline = new();
/// preProcessorPipeline.AddProcessor(new CommentsPreProcessor());
/// preProcessorPipeline.AddProcessor(new VictorianFlourishPreProcessor());
/// 
/// string sourceCode = File.ReadAllText("programme.topsy");
/// PreProcessResult result = preProcessorPipeline.Execute(sourceCode);
/// string transformedCode = result.TransformedText;
/// SourceMap sourceMap = result.SourceMap;
/// </code>
/// </para>
/// </remarks>
public class PreProcessorPipeline
{
    private readonly List<ITopsyTurvyPreProcessor> processors = [];

    /// <summary>
    /// Appends a pre-processor to the end of the pipeline.
    /// </summary>
    /// <param name="processor">The pre-processor to add.</param>
    public void AddProcessor(ITopsyTurvyPreProcessor processor) =>
        this.processors.Add(processor);

    /// <summary>
    /// Executes every registered pre-processor in order.
    /// </summary>
    /// <param name="input">The raw source code.</param>
    /// <returns>A <see cref="PreProcessResult"/> containing the transformed text and source map.</returns>
    public PreProcessResult Execute(string input)
    {
        SourceMap currentMap = new();
        string currentText = input;

        foreach (ITopsyTurvyPreProcessor processor in this.processors)
        {
            PreProcessResult result = processor.Process(currentText, currentMap);
            currentText = result.TransformedText;
        }

        return new(currentText, currentMap);
    }
}
