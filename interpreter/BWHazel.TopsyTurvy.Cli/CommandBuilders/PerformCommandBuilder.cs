using System.CommandLine;
using BWHazel.TopsyTurvy.Runtime;

namespace BWHazel.TopsyTurvy.Cli.CommandBuilders;

/// <summary>
/// Builder for the <c>perform</c> command.
/// </summary>
public static class PerformCommandBuilder
{
    /// <summary>
    /// Builds and configures the <c>perform</c> command.
    /// </summary>
    /// <returns>A configured <see cref="Command"/> instance.</returns>
    public static Command Build()
    {
        Command performCommand = new("perform", "Executes a Topsy Turvy .topsy file.");
        performCommand.Aliases.Add("stage");
        performCommand.Aliases.Add("run");

        Argument<string> fileArgument = new("filename")
        {
            Description = "The Topsy Turvy file to perform."
        };

        Option<bool> tiptoeOption = new("--tiptoe")
        {
            Description = "Only prints program output and thus chooses to discard aestheticism."
        };

        performCommand.Arguments.Add(fileArgument);
        performCommand.Options.Add(tiptoeOption);

        performCommand.SetAction(parseResult =>
            HandlePerform(
                parseResult.GetValue(fileArgument) ?? string.Empty,
                parseResult.GetValue(tiptoeOption)));

        return performCommand;
    }

    /// <summary>
    /// Handles the execution of the <c>perform</c> command.
    /// </summary>
    /// <param name="filename">The filename of the Topsy Turvy file to perform.</param>
    /// <param name="tiptoe">A value indicating whether to only print program output.</param>
    /// <returns>An integer exit code with 0 for success or 1 for failure.</returns>
    private static int HandlePerform(string filename, bool tiptoe)
    {
        if (!tiptoe)
        {
            PanelHelper.WriteDefault("Now Performing...", $"[cyan]{filename}[/]");
        }

        ConsoleIO io = new();
        ProgramExecutionResult result = ProgramRunner.Run(filename, io);

        if (result.IsSuccess)
        {
            if (!tiptoe)
            {
                PanelHelper.WriteSuccess("Performance Over!", "[lightgreen_1]And a Good Job Too![/]");
            }

            return 0;
        }

        PanelHelper.ReportErrors(result, tiptoe);
        return 1;
    }
}
