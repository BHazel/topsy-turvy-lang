using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Parser;

/// <summary>
/// Runs a sequential chain of <see cref="ITopsyTurvyPreProcessor"/> instances over source text.
/// </summary>
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
            currentText = result.Text;
        }

        return new(currentText, currentMap);
    }
}
