using System;
using System.CommandLine;
using System.Linq;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Runtime;
using Spectre.Console;

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

        performCommand.SetAction(async parseResult =>
        {
            await HandlePerform(
                parseResult.GetValue(fileArgument) ?? string.Empty,
                parseResult.GetValue(tiptoeOption));
        });

        return performCommand;
    }

    /// <summary>
    /// Handles the execution of the <c>perform</c> command.
    /// </summary>
    /// <param name="filename">The filename of the Topsy Turvy file to perform.</param>
    /// <param name="tiptoe">A value indicating whether to only print program output.</param>
    /// <returns>A task representing the asynchronous operation, with an integer result indicating the exit code.</returns>
    private static async Task<int> HandlePerform(string filename, bool tiptoe)
    {
        ProgramRunner runner = new();
        ConsoleIO io = new();
        ProgramExecutionResult result = null!;

        if (!tiptoe)
        {
            Panel startPanel = new($"[cyan]{filename}[/]")
            {
                BorderStyle = new Style(Color.Cyan),
                Header = new PanelHeader("Now Performing...")
            };

            AnsiConsole.Write(startPanel);
        }

        result = ProgramRunner.Run(filename, io);
        if (result == null)
        {
            return 1;
        }

        if (result.IsSuccess)
        {
            if (!tiptoe)
            {
                Panel successPanel = new("[lightgreen_1]Oh Joy, Oh Rapture Unforseen![/]")
                {
                    BorderStyle = new Style(Color.LightGreen_1),
                    Header = new PanelHeader("Performance Complete!")
                };

                AnsiConsole.Write(successPanel);
            }
                
            return 0;
        }

        if (tiptoe)
        {
            ReportErrorsTiptoe(result);
        }
        else
        {
            ReportErrorsRich(result);
        }

        return 1;
    }

    /// <summary>
    /// Reports errors without any formatting.
    /// </summary>
    /// <param name="result">The result of the program execution containing errors.</param>
    private static void ReportErrorsTiptoe(ProgramExecutionResult result)
    {
        if (result.ErrorMessage != null)
        {
            Console.Error.WriteLine(result.ErrorMessage);
        }

        if (result.SyntaxErrors != null)
        {
            foreach (string error in result.SyntaxErrors)
            {
                Console.Error.WriteLine(error);
            }
        }

        if (result.RuntimeDiagnostics != null)
        {
            foreach (Diagnostic diagnostic in result.RuntimeDiagnostics)
            {
                Console.Error.WriteLine($"[{diagnostic.Span.Start.Line}:{diagnostic.Span.Start.Column}] {diagnostic.Message}");
            }
        }
    }

    /// <summary>
    /// Reports errors with rich formatting.
    /// </summary>
    /// <param name="result">The result of the program execution containing errors.</param>
    private static void ReportErrorsRich(ProgramExecutionResult result)
    {
        if (result.ErrorMessage != null)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {result.ErrorMessage}");
        }

        if (result.SyntaxErrors != null)
        {
            Panel panel = new(new Markup($"[red]Syntax Error: {string.Join("\n", result.SyntaxErrors)}[/]"))
            {
                BorderStyle = new Style(Color.Red),
                Header = new PanelHeader("Crushed Again!")
            };

            AnsiConsole.Write(panel);
        }

        if (result.RuntimeDiagnostics != null)
        {
            Panel panel = new(new Markup($"[red]Runtime Error:\n{string.Join("\n", result.RuntimeDiagnostics.Select(d => $"[[{d.Span.Start.Line}:{d.Span.Start.Column}]] {d.Message}"))}[/]"))
            {
                BorderStyle = new Style(Color.Red),
                Header = new PanelHeader("A Hideous Curse!")
            };
                
            AnsiConsole.Write(panel);
        }
    }
}
