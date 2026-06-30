using System;
using System.CommandLine;
using System.Text.Json;
using Spectre.Console;
using Spectre.Console.Json;

using TopsyParseResult = BWHazel.TopsyTurvy.Parser.ParseResult;

namespace BWHazel.TopsyTurvy.Cli.CommandBuilders;

/// <summary>
/// Builder for the <c>promptbook</c> subcommand of <c>sorcerer</c>.
/// </summary>
public static class PromptbookCommandBuilder
{
    /// <summary>
    /// Builds and configures the <c>promptbook</c> subcommand.
    /// </summary>
    /// <returns>A configured <see cref="Command"/> instance.</returns>
    public static Command Build()
    {
        Command promptbookCommand = new("promptbook", "Prints out the Abstract Syntax Tree of a Topsy Turvy .topsy file.");
        promptbookCommand.Aliases.Add("ast");

        Argument<string> fileArgument = new("filename")
        {
            Description = "The Topsy Turvy file to inspect."
        };

        Option<bool> abridgedOption = new("--abridged")
        {
            Description = "Removes all whitespace from the JSON output."
        };

        abridgedOption.Aliases.Add("-a");

        Option<bool> chromaticOption = new("--chromatic")
        {
            Description = "Syntax highlights the JSON output."
        };

        chromaticOption.Aliases.Add("-c");

        Option<bool> tiptoeOption = new("--tiptoe")
        {
            Description = "Only prints program output and thus chooses to discard aestheticism."
        };

        promptbookCommand.Arguments.Add(fileArgument);
        promptbookCommand.Options.Add(abridgedOption);
        promptbookCommand.Options.Add(chromaticOption);
        promptbookCommand.Options.Add(tiptoeOption);

        promptbookCommand.SetAction(parseResult =>
            HandlePromptbook(
                parseResult.GetValue(fileArgument) ?? string.Empty,
                parseResult.GetValue(abridgedOption),
                parseResult.GetValue(chromaticOption),
                parseResult.GetValue(tiptoeOption)));

        return promptbookCommand;
    }

    /// <summary>
    /// Handles the execution of the <c>promptbook</c> subcommand.
    /// </summary>
    /// <param name="filename">The filename of the Topsy Turvy file to inspect.</param>
    /// <param name="abridged">A value indicating whether to compact the JSON output.</param>
    /// <param name="chromatic">A value indicating whether to syntax-highlight the JSON output.</param>
    /// <param name="tiptoe">A value indicating whether to only print program output.</param>
    /// <returns>An integer exit code with 0 for success or 1 for failure.</returns>
    private static int HandlePromptbook(string filename, bool abridged, bool chromatic, bool tiptoe)
    {
        if (!tiptoe)
        {
            PanelHelper.WriteDefault("Consulting the Promptbook...", $"[cyan]{filename}[/]");
        }

        (ProgramExecutionResult result, TopsyParseResult? parseData) = ProgramRunner.ParseFile(filename);
        if (!result.IsSuccess)
        {
            PanelHelper.ReportErrors(result, tiptoe);
            return 1;
        }

        JsonSerializerOptions jsonOptions = new()
        {
            WriteIndented = !abridged,
            Converters =
            {
                new NodeJsonConverter()
            }
        };

        string astJson = JsonSerializer.Serialize(parseData!.Program, jsonOptions);
        if (chromatic)
        {
            AnsiConsole.Write(new JsonText(astJson));
            Console.WriteLine();
        }
        else
        {
            Console.WriteLine(astJson);
        }

        return 0;
    }
}
