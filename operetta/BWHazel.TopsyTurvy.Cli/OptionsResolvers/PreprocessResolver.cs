using System;
using System.Collections.Generic;
using BWHazel.TopsyTurvy.Parser;

namespace BWHazel.TopsyTurvy.Cli.OptionsResolvers;

/// <summary>
/// Resolves and performs the <c>--emit preprocess</c> emit to write the pre-processed Topsy Turvy source text to STDOUT.
/// </summary>
/// <remarks>
/// ### Command-Line Configuration
/// _None_
/// 
/// ### Additional Configuration
/// _None_
/// </remarks>
public sealed class PreprocessResolver : IEmitterOptionsResolver<NoOptions>
{
    /// <inheritdoc/>
    public IReadOnlyCollection<string> GetAllowedExtensions() => [FileManager.TopsyTurvyFileExtension];

    /// <inheritdoc/>
    public NoOptions Apply(NoOptions baseOptions, IReadOnlyDictionary<string, string> config, bool tiptoe)
    {
        if (config.Count > 0)
        {
            PanelHelper.ReportUserWarning(tiptoe, $"--emit preprocess does not use any --config keys; ignoring: {string.Join(", ", config.Keys)}.");
        }

        return baseOptions;
    }

    /// <inheritdoc/>
    public int Emit(string filename, NoOptions options, bool tiptoe)
    {
        (bool success, string? errorMessage) = FileManager.TryReadSource(filename, out string source);
        if (!success)
        {
            PanelHelper.ReportErrors(ProgramExecutionResult.Failure(errorMessage!), tiptoe);
            return 1;
        }

        PreProcessorPipeline pipeline = new();
        pipeline.AddProcessor(new CommentsPreProcessor());
        pipeline.AddProcessor(new VictorianFlourishPreProcessor());
        PreProcessResult result = pipeline.Execute(source);
        Console.Write(result.TransformedText);

        return 0;
    }
}
