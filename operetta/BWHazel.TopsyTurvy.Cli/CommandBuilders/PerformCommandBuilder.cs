using System.CommandLine;
using BWHazel.TopsyTurvy.StandardLibrary.IO;

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

        Argument<string[]> commandLineArgsArgument = new("arguments")
        {
            Description = "Command-line arguments to pass in.",
            Arity = ArgumentArity.ZeroOrMore
        };

        Option<bool> tiptoeOption = new("--tiptoe")
        {
            Description = "Only prints program output and thus chooses to discard aestheticism."
        };

        Option<string[]> admitOption = new("--admit")
        {
            Description = "Loads an external .NET assembly (.dll) so its functions become callable via SUMMON.  Can be specified multiple times."
        };

        admitOption.Aliases.Add("--include");

        performCommand.Arguments.Add(fileArgument);
        performCommand.Arguments.Add(commandLineArgsArgument);
        performCommand.Options.Add(tiptoeOption);
        performCommand.Options.Add(admitOption);

        performCommand.SetAction(parseResult =>
            HandlePerform(
                parseResult.GetValue(fileArgument) ?? string.Empty,
                parseResult.GetValue(tiptoeOption),
                parseResult.GetValue(commandLineArgsArgument),
                parseResult.GetValue(admitOption) ?? []));

        return performCommand;
    }

    /// <summary>
    /// Handles the execution of the <c>perform</c> command.
    /// </summary>
    /// <param name="filename">The filename of the Topsy Turvy file to perform.</param>
    /// <param name="tiptoe">A value indicating whether to only print program output.</param>
    /// <param name="commandLineArguments">Command-line arguments to pass to the program, or <c>null</c> for none.</param>
    /// <param name="externalLibraryAssemblyPaths">The paths to external library assemblies to admit, or empty for none.</param>
    /// <returns>An integer exit code with 0 for success or 1 for failure.</returns>
    private static int HandlePerform(string filename, bool tiptoe, string[]? commandLineArguments, string[] externalLibraryAssemblyPaths)
    {
        ExternalLibraryLoadResult loadResult = ExternalLibraryLoader.Load(externalLibraryAssemblyPaths);
        if (loadResult.ErrorMessage is not null)
        {
            return PanelHelper.ReportUserError(tiptoe, loadResult.ErrorMessage);
        }

        if (!tiptoe)
        {
            PanelHelper.WriteDefault("Now Performing...", $"[cyan]{filename}[/]");
        }

        ConsoleIO io = new();
        ProgramExecutionResult result = ProgramRunner.Run(filename, io, commandLineArguments: commandLineArguments, externalFunctions: loadResult.Catalogue);

        if (result.IsSuccess)
        {
            if (!tiptoe)
            {
                PanelHelper.WriteSuccess("Performance Over!", "[lightgreen_1]And a Good Job Too![/]");
            }

            return result.ExitCode;
        }

        PanelHelper.ReportErrors(result, tiptoe);
        return 1;
    }
}
