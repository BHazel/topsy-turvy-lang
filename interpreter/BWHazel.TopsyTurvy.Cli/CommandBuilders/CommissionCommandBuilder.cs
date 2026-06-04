using System;
using System.CommandLine;

namespace BWHazel.TopsyTurvy.Cli.CommandBuilders;

/// <summary>
/// Builder for the <c>commission</c> command.
/// </summary>
public static class CommissionCommandBuilder
{
    /// <summary>
    /// Builds and configures the <c>commission</c> command.
    /// </summary>
    /// <returns>A configured <see cref="Command"/> instance.</returns>
    public static Command Build()
    {
        Command commissionCommand = new("commission", "Creates a new Topsy Turvy .topsy file with a Hello, World programme.");
        commissionCommand.Aliases.Add("new");

        Argument<string> fileArgument = new("filename")
        {
            Description = "The Topsy Turvy file to create."
        };

        Option<string?> titleOption = new("--title")
        {
            Description = $"The title of the programme.  Defaults to \"{FileManager.DefaultProgrammeTitle}\"."
        };

        titleOption.Aliases.Add("-t");

        Option<string?> orOption = new("--or")
        {
            Description = "The optional subtitle of the programme."
        };

        Option<bool> tiptoeOption = new("--tiptoe")
        {
            Description = "Only prints program output and thus chooses to discard aestheticism."
        };

        commissionCommand.Arguments.Add(fileArgument);
        commissionCommand.Options.Add(titleOption);
        commissionCommand.Options.Add(orOption);
        commissionCommand.Options.Add(tiptoeOption);

        commissionCommand.SetAction(parseResult =>
            HandleCommission(
                parseResult.GetValue(fileArgument) ?? string.Empty,
                parseResult.GetValue(titleOption) ?? FileManager.DefaultProgrammeTitle,
                parseResult.GetValue(orOption),
                parseResult.GetValue(tiptoeOption)));

        return commissionCommand;
    }

    /// <summary>
    /// Handles the execution of the <c>commission</c> command.
    /// </summary>
    /// <param name="filename">The filename of the Topsy Turvy file to create.</param>
    /// <param name="title">The programme title.</param>
    /// <param name="subtitle">The optional programme subtitle.</param>
    /// <param name="tiptoe">A value indicating whether to only print program output.</param>
    /// <returns>An integer exit code with 0 for success or 1 for failure.</returns>
    private static int HandleCommission(string filename, string title, string? subtitle, bool tiptoe)
    {
        if (!filename.EndsWith(FileManager.FileExtension, StringComparison.OrdinalIgnoreCase))
        {
            string message = $"Invalid file extension: '{filename}'. Only {FileManager.FileExtension} files are supported.";
            if (tiptoe)
            {
                Console.Error.WriteLine(message);
            }
            else
            {
                PanelHelper.WriteUserError(message);
            }

            return 1;
        }

        (bool success, string? errorMessage) = FileManager.TryCreateProgrammeFile(filename, title, subtitle);
        if (!success)
        {
            if (tiptoe)
            {
                Console.Error.WriteLine(errorMessage);
            }
            else
            {
                PanelHelper.WriteUserError(errorMessage!);
            }

            return 1;
        }

        if (!tiptoe)
        {
            PanelHelper.WriteSuccess("Commissioned!", $"[cyan]{filename}[/]");
        }

        return 0;
    }
}
