using System.CommandLine;

namespace BWHazel.TopsyTurvy.Cli.CommandBuilders;

/// <summary>
/// Builder for the <c>rehearse</c> command.
/// </summary>
public static class RehearseCommandBuilder
{
    /// <summary>
    /// Builds and configures the <c>rehearse</c> command.
    /// </summary>
    /// <returns>A configured <see cref="Command"/> instance.</returns>
    public static Command Build()
    {
        Command rehearseCommand = new("rehearse", "Checks a Topsy Turvy .topsy file for syntax errors.");
        rehearseCommand.Aliases.Add("check");

        Argument<string> fileArgument = new("filename")
        {
            Description = "The Topsy Turvy file to rehearse."
        };

        Option<bool> tiptoeOption = new("--tiptoe")
        {
            Description = "Only prints program output and thus chooses to discard aestheticism."
        };

        rehearseCommand.Arguments.Add(fileArgument);
        rehearseCommand.Options.Add(tiptoeOption);

        rehearseCommand.SetAction(parseResult =>
            HandleRehearse(
                parseResult.GetValue(fileArgument) ?? string.Empty,
                parseResult.GetValue(tiptoeOption)));

        return rehearseCommand;
    }

    /// <summary>
    /// Handles the execution of the <c>rehearse</c> command.
    /// </summary>
    /// <param name="filename">The filename of the Topsy Turvy file to rehearse.</param>
    /// <param name="tiptoe">A value indicating whether to only print program output.</param>
    /// <returns>An integer exit code with 0 for success or 1 for failure.</returns>
    private static int HandleRehearse(string filename, bool tiptoe)
    {
        if (!tiptoe)
        {
            PanelHelper.WriteDefault("Now Rehearsing...", $"[cyan]{filename}[/]");
        }

        ProgramExecutionResult result = ProgramRunner.Check(filename);
        if (result.IsSuccess)
        {
            if (!tiptoe)
            {
                PanelHelper.WriteSuccess("Rehearsal Over!", "[lightgreen_1]Oh Joy, Oh Rapture Unforseen[/]");
            }

            return 0;
        }

        PanelHelper.ReportErrors(result, tiptoe);
        return 1;
    }
}
