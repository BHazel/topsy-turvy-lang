using System;
using System.CommandLine;
using BWHazel.TopsyTurvy.Parser;

namespace BWHazel.TopsyTurvy.Cli.CommandBuilders;

/// <summary>
/// Builder for the <c>cue</c> subcommand of <c>sorcerer</c>.
/// </summary>
public static class CueCommandBuilder
{
    /// <summary>
    /// Builds and configures the <c>cue</c> subcommand.
    /// </summary>
    /// <returns>A configured <see cref="Command"/> instance.</returns>
    public static Command Build()
    {
        Command cueCommand = new("cue", "Runs the pre-processor for a Topsy Turvy .topsy file and outputs the result.");
        cueCommand.Aliases.Add("preprocess");

        Argument<string> fileArgument = new("filename")
        {
            Description = "The Topsy Turvy file to pre-process."
        };

        Option<bool> tiptoeOption = new("--tiptoe")
        {
            Description = "Only prints the pre-processed output and thus chooses to discard aestheticism."
        };

        cueCommand.Arguments.Add(fileArgument);
        cueCommand.Options.Add(tiptoeOption);

        cueCommand.SetAction(parseResult =>
            HandleCue(
                parseResult.GetValue(fileArgument) ?? string.Empty,
                parseResult.GetValue(tiptoeOption)));

        return cueCommand;
    }

    /// <summary>
    /// Handles the execution of the <c>cue</c> subcommand.
    /// </summary>
    /// <param name="filename">The filename of the Topsy Turvy file to pre-process.</param>
    /// <param name="tiptoe">A value indicating whether to only print the pre-processed output.</param>
    /// <returns>An integer exit code with 0 for success or 1 for failure.</returns>
    private static int HandleCue(string filename, bool tiptoe)
    {
        if (!tiptoe)
        {
            PanelHelper.WriteDefault("Cueing...", $"[cyan]{filename}[/]");
        }

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
