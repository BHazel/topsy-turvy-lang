using System;
using System.Collections.Generic;
using System.Linq;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Parser;
using BWHazel.TopsyTurvy.Runtime;
using Spectre.Console;

namespace BWHazel.TopsyTurvy.Cli.Repl;

/// <summary>
/// Orchestrates the interactive REPL session, managing the shared execution environment,
/// input reading, preprocessing, parsing, execution and output rendering.
/// </summary>
/// <remarks>
/// <para>
/// The session maintains a single <see cref="TopsyTurvyEnvironment"/> and <see cref="Interpreter"/>
/// instance across all REPL calls so that variables and function definitions declared in one call
/// are available in subsequent calls.
/// </para>
/// <para>
/// Each call of user input is wrapped in a minimal <c>HARK! ... FINALE.</c> programme before parsing,
/// allowing both declarations and statements to be entered without requiring the user to supply
/// programme structure keywords.
/// </para>
/// <para>
/// In single-line and `tiptoe` modes the session detects incomplete block constructs, e.g. an open
/// <c>SHOULD IT TRANSPIRE THAT</c> block, by attempting a parse after each submitted line.  If all parse
/// errors are at the <c>FINALE.</c> line, the block is assumed to be still open and a continuation
/// prompt is shown until the block closes cleanly.
/// </para>
/// </remarks>
public sealed class ReplSession
{
    private const string ReplProgramTitle = "Cadenza";

    private readonly TopsyTurvyParser parser = new();
    private readonly TopsyTurvyEnvironment sessionEnvironment = TopsyTurvyEnvironment.CreateGlobal();
    private readonly List<string> history = [];
    private ReplIO io = new();

    /// <summary>
    /// Runs the REPL loop until the user exits.
    /// </summary>
    /// <param name="tiptoe">When <c>true</c>, uses plain-text I/O without styling; when <c>false</c>, renders styled output.</param>
    public void Run(bool tiptoe)
    {
        bool isInteractive = !Console.IsInputRedirected;
        this.io = new ReplIO(isTiptoe: tiptoe, isInteractive: isInteractive);
        Interpreter interpreter = new(this.io);

        // Set up THE PROPS as an empty array: no command-line arguments in the REPL.
        this.sessionEnvironment.Declare(Keywords.SpecialNames.TheProps, TopsyTurvyValue.Array([]), isConstant: true);
        bool isMultiLineMode = false;
        if (isInteractive && !tiptoe)
        {
            WriteWelcomeBanner();
        }

        // Both rich and `tiptoe` interactive modes use CadenzaInputReader so that history
        // navigation and cursor movement are available in all interactive contexts.
        ReplInputReader? inputReader = isInteractive
            ? new(this.history, isTiptoe: tiptoe)
            : null;

        while (true)
        {
            string userInput = ReadInput(inputReader, isMultiLineMode);
            if (userInput is ReplConstants.Exit or ReplConstants.Quit)
            {
                break;
            }

            if (userInput is ReplConstants.Begone or ReplConstants.Clear)
            {
                if (tiptoe || !isInteractive)
                {
                    Console.Clear();
                }
                else
                {
                    AnsiConsole.Clear();
                }

                continue;
            }

            if (userInput is ReplConstants.Entracte or ReplConstants.Help)
            {
                WriteHelpTable(tiptoe || !isInteractive);
                continue;
            }

            if (userInput is ReplConstants.Madrigal or ReplConstants.MultiLine)
            {
                isMultiLineMode = true;
                if (!tiptoe && isInteractive)
                {
                    AnsiConsole.MarkupLine("[dim]Switched to multi-line mode. Press Enter on a blank line to execute.[/]");
                }

                continue;
            }

            if (userInput is ReplConstants.Patter or ReplConstants.SingleLine)
            {
                isMultiLineMode = false;
                if (!tiptoe && isInteractive)
                {
                    AnsiConsole.MarkupLine("[dim]Switched to single-line mode.[/]");
                }

                continue;
            }

            if (string.IsNullOrWhiteSpace(userInput))
            {
                continue;
            }

            // In single-line and `tiptoe` modes, auto-detect open block constructs and show a
            // continuation prompt until the accumulated input parses without FINALE.-level errors.
            if (!isMultiLineMode && inputReader is not null)
            {
                userInput = this.AccumulateUntilComplete(userInput, inputReader);

                // An exit may be returned if the user pressed Ctrl+C during continuation.
                if (userInput is ReplConstants.Exit or ReplConstants.Quit)
                {
                    break;
                }

                if (userInput is ReplConstants.Begone or ReplConstants.Clear)
                {
                    if (tiptoe || !isInteractive)
                    {
                        Console.Clear();
                    }
                    else
                    {
                        AnsiConsole.Clear();
                    }

                    continue;
                }

                if (string.IsNullOrWhiteSpace(userInput))
                {
                    continue;
                }
            }

            if (isInteractive)
            {
                this.history.Add(userInput);
            }

            this.EvaluateInput(userInput, interpreter, tiptoe, isInteractive);
        }
    }

    /// <summary>
    /// Reads the next input from the appropriate source based on mode and interactivity.
    /// </summary>
    private static string ReadInput(ReplInputReader? inputReader, bool isMultiLineMode)
    {
        if (inputReader is not null)
        {
            return inputReader.ReadInput(isMultiLineMode);
        }

        // Non-interactive (piped stdin): plain ReadLine with no prompt.
        return Console.ReadLine() ?? ReplConstants.Exit;
    }

    /// <summary>
    /// Accumulates continuation lines until the combined input constitutes a syntactically complete
    /// block, or until an exit or clear command is returned by the reader.
    /// </summary>
    /// <param name="initialInput">The first line already read from the user.</param>
    /// <param name="inputReader">The reader used to obtain continuation lines.</param>
    /// <returns>
    /// The accumulated multi-line input once it parses as a complete block, or a control command
    /// if the user interrupted continuation.
    /// </returns>
    private string AccumulateUntilComplete(string initialInput, ReplInputReader inputReader)
    {
        string accumulatedInput = initialInput;
        while (this.LooksIncomplete(accumulatedInput))
        {
            string continuationLine = inputReader.ReadContinuationLine();

            if (continuationLine is ReplConstants.Exit or ReplConstants.Quit or ReplConstants.Begone or ReplConstants.Clear)
            {
                return continuationLine;
            }

            accumulatedInput = accumulatedInput + "\n" + continuationLine;
        }

        return accumulatedInput;
    }

    /// <summary>
    /// Returns <c>true</c> when <paramref name="rawUserInput"/> wraps into a programme that fails to parse
    /// solely because of errors at or after the <c>FINALE.</c> line, indicating an open, unclosed block.
    /// </summary>
    private bool LooksIncomplete(string rawUserInput)
    {
        PreProcessResult preprocessed = BuildPreProcessor().Execute(rawUserInput);

        string source = $"HARK! \"{ReplProgramTitle}\"\n{preprocessed.TransformedText}\nFINALE.";
        ParseResult result = this.parser.TryParse(source);

        if (result.Success)
        {
            return false;
        }

        // FINALE. is the last line in the wrapped source.
        int finaleLine = source.Split('\n').Length;
        return result.Diagnostics.All(diagnostic => diagnostic.Span.Start.Line >= finaleLine);
    }

    /// <summary>
    /// Preprocesses, parses, and executes a single REPL turn, then renders the result.
    /// </summary>
    private void EvaluateInput(string rawInput, Interpreter interpreter, bool tiptoe, bool isInteractive)
    {
        PreProcessResult preprocessed = BuildPreProcessor().Execute(rawInput);

        // Wrap in minimal programme structure.
        string source = $"HARK! \"{ReplProgramTitle}\"\n{preprocessed.TransformedText}\nFINALE.";

        ParseResult parseResult = this.parser.TryParse(source);
        if (!parseResult.Success)
        {
            IEnumerable<string> errors = parseResult.Diagnostics
                .Select(diagnostic => $"[{diagnostic.Span.Start.Line}:{diagnostic.Span.Start.Column}] {diagnostic.Message}");

            if (tiptoe || !isInteractive)
            {
                foreach (string error in errors)
                {
                    Console.Error.WriteLine(error);
                }
            }
            else
            {
                PanelHelper.WriteSyntaxErrors(errors);
            }

            return;
        }

        DiagnosticCollection diagnostics = interpreter.Execute(
            parseResult.Program!,
            sessionEnvironment: this.sessionEnvironment);

        IReadOnlyList<string> outputLines = this.io.FlushOutput();
        if (tiptoe || !isInteractive)
        {
            foreach (string line in outputLines)
            {
                Console.WriteLine(line);
            }

            if (diagnostics.HasErrors)
            {
                foreach (Diagnostic diagnostic in diagnostics.Diagnostics)
                {
                    Console.Error.WriteLine($"[{diagnostic.Span.Start.Line}:{diagnostic.Span.Start.Column}] {diagnostic.Message}");
                }
            }

            return;
        }

        // In rich mode echo input then show output or error panels.
        WriteInputPanel(rawInput);

        if (diagnostics.HasErrors)
        {
            PanelHelper.WriteRuntimeErrors(diagnostics.Diagnostics);
        }
        else if (outputLines.Count > 0)
        {
            WriteOutputPanel(outputLines);
        }
        else
        {
            AnsiConsole.MarkupLine("[dim green]✓[/]");
        }
    }

    /// <summary>
    /// Writes the "In" panel echoing the submitted source text.
    /// </summary>
    private static void WriteInputPanel(string source)
    {
        string escaped = Markup.Escape(source);
        Panel panel = new(new Markup($"[grey]{escaped}[/]"))
        {
            Expand = true,
            BorderStyle = new(Color.Grey),
            Header = new("In")
        };

        AnsiConsole.Write(panel);
    }

    /// <summary>
    /// Writes the "Out" panel showing programme output.
    /// </summary>
    private static void WriteOutputPanel(IReadOnlyList<string> outputLines)
    {
        string content = string.Join("\n", outputLines.Select(l => $"[lightgreen_1]{Markup.Escape(l)}[/]"));
        Panel panel = new(new Markup(content))
        {
            Expand = true,
            BorderStyle = new(Color.LightGreen_1),
            Header = new("Out")
        };

        AnsiConsole.Write(panel);
    }

    /// <summary>
    /// Writes the welcome banner shown when the REPL starts in interactive rich mode.
    /// </summary>
    private static void WriteWelcomeBanner()
    {
        Panel panel = new(new Markup(
            "[cyan]Topsy Turvy Interactive REPL[/]\n" +
            "[dim]Type [bold]:entracte[/] for help or [bold]:exit[/] to quit.[/]"))
        {
            BorderStyle = new(Color.Cyan),
            Header = new("Cadenza")
        };

        AnsiConsole.Write(panel);
    }

    /// <summary>
    /// Writes the REPL command help table.
    /// </summary>
    private static void WriteHelpTable(bool plainText)
    {
        if (plainText)
        {
            Console.WriteLine($"{ReplConstants.Exit} / {ReplConstants.Quit}            Exit the REPL");
            Console.WriteLine($"{ReplConstants.Begone} / {ReplConstants.Clear}         Clear the screen");
            Console.WriteLine($"{ReplConstants.Madrigal} / {ReplConstants.MultiLine}   Switch to multi-line mode");
            Console.WriteLine($"{ReplConstants.Patter} / {ReplConstants.SingleLine}    Switch to single-line mode");
            Console.WriteLine($"{ReplConstants.Entracte} / {ReplConstants.Help}        Show this help");
            return;
        }

        Table table = new()
        {
            Border = TableBorder.Rounded,
            BorderStyle = new Style(Color.DeepSkyBlue1)
        };

        table.AddColumn(new("[bold]Command[/]"));
        table.AddColumn(new("[bold]Alias[/]"));
        table.AddColumn(new("[bold]Description[/]"));

        table.AddRow(ReplConstants.Exit, ReplConstants.Quit, "Exit the REPL");
        table.AddRow(ReplConstants.Begone, ReplConstants.Clear, "Clear the screen");
        table.AddRow(ReplConstants.Madrigal, ReplConstants.MultiLine, "Switch to multi-line mode (blank line executes)");
        table.AddRow(ReplConstants.Patter, ReplConstants.SingleLine, "Switch to single-line mode (Enter executes)");
        table.AddRow(ReplConstants.Entracte, ReplConstants.Help, "Show this help table");

        AnsiConsole.Write(table);
    }

    /// <summary>
    /// Builds and returns a configured preprocessor pipeline.
    /// </summary>
    private static PreProcessorPipeline BuildPreProcessor()
    {
        PreProcessorPipeline pipeline = new();
        pipeline.AddProcessor(new CommentsPreProcessor());
        pipeline.AddProcessor(new VictorianFlourishPreProcessor());
        return pipeline;
    }
}
