using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Debugger;
using BWHazel.TopsyTurvy.Runtime;
using Spectre.Console;

namespace BWHazel.TopsyTurvy.Cli.Debug;

/// <summary>
/// The interactive terminal front end for the debugger.
/// </summary>
/// <remarks>
/// Reads a line at a time at the <c>(director)</c> prompt, dispatches it via <see cref="DebugConsoleCommandParser"/>
/// and calls the matching <see cref="DebugSession"/> member directly.  A resume-family command (<c>proceed</c>/
/// <c>step</c>/<c>enter</c>/<c>exit</c>) blocks the console loop until the next <see cref="DebugSession.Paused"/> or
/// <see cref="DebugSession.Ended"/> event of the session, so the next prompt is only shown once there is something
/// new to act on.
/// </remarks>
internal sealed class DebugConsole
{
    private const string PromptColour = "bold deepskyblue1";
    private const string PauseColour = "yellow";
    private const string ErrorColour = "red";
    private const string BreakpointCreatedColour = "purple";
    private const string BreakpointRemovedColour = "orange1";
    private const string ArmouryTypeColour = "cyan";
    private const string ArmouryValueColour = "lightgreen_1";
    private const string EntracteCommandColour = "cyan";
    private const string EntracteAliasColour = "lightgreen_1";

    private readonly DebugSession session;
    private readonly bool tiptoe;
    private readonly SemaphoreSlim awaitingNextEvent = new(0, 1);
    private int selectedFrameId;
    private bool lastEndedHadErrors;

    /// <summary>
    /// Initialises a new instance of the <see cref="DebugConsole"/> class.
    /// </summary>
    /// <param name="session">The session to drive.</param>
    /// <param name="tiptoe">A value indicating whether to suppress panels and colours.</param>
    internal DebugConsole(DebugSession session, bool tiptoe)
    {
        this.session = session;
        this.tiptoe = tiptoe;
        this.session.Paused += this.OnPaused;
        this.session.Output += this.OnOutput;
        this.session.Ended += this.OnEnded;
    }

    /// <summary>
    /// Starts the session against <paramref name="sourceFilePath"/> and runs the interactive console loop until the
    /// the session is ended or the target programme completes.
    /// </summary>
    /// <param name="sourceFilePath">The <c>.topsy</c> file to debug.</param>
    /// <param name="cancellationToken">A token that, when cancelled, ends the session.</param>
    /// <returns>An exit code: <c>0</c> if the programme completed with no errors, otherwise <c>1</c>.</returns>
    internal async Task<int> RunAsync(string sourceFilePath, CancellationToken cancellationToken)
    {
        try
        {
            await this.session.StartAsync(sourceFilePath, cancellationToken);
        }
        catch (DebugSessionStartException ex)
        {
            return PanelHelper.ReportUserError(this.tiptoe, ex.Message);
        }

        if (this.tiptoe)
        {
            Console.WriteLine($"Loaded '{sourceFilePath}'.");
        }
        else
        {
            PanelHelper.WriteDefault("Directing...", $"[cyan]{sourceFilePath}[/]");
        }

        this.WriteLine("Type 'entracte' for a list of commands.");

        while (this.session.Status != SessionStatus.Ended)
        {
            this.WritePrompt();
            string? line = Console.ReadLine();
            if (line is null)
            {
                this.session.Stop();
                break;
            }

            this.Dispatch(line);
        }

        return this.lastEndedHadErrors
            ? 1
            : 0;
    }

    /// <summary>
    /// Dispatches one parsed console command to the matching <see cref="DebugSession"/> member.
    /// </summary>
    /// <param name="line">The raw input line.</param>
    private void Dispatch(string line)
    {
        DebugConsoleCommand command = DebugConsoleCommandParser.Parse(line);
        try
        {
            switch (command.Kind)
            {
                case DebugConsoleCommandKind.Mark:
                    this.HandleMark(command.Argument);
                    break;
                case DebugConsoleCommandKind.Unmark:
                    this.HandleUnmark(command.Argument);
                    break;
                case DebugConsoleCommandKind.Proceed:
                    this.ResumeAndWait(this.session.Continue);
                    break;
                case DebugConsoleCommandKind.Step:
                    this.ResumeAndWait(this.session.StepOver);
                    break;
                case DebugConsoleCommandKind.Enter:
                    this.ResumeAndWait(this.session.StepIn);
                    break;
                case DebugConsoleCommandKind.Exit:
                    this.ResumeAndWait(this.session.StepOut);
                    break;
                case DebugConsoleCommandKind.Pause:
                    this.session.Pause();
                    break;
                case DebugConsoleCommandKind.Troupe:
                    this.HandleTroupe();
                    break;
                case DebugConsoleCommandKind.Cue:
                    this.HandleCue();
                    break;
                case DebugConsoleCommandKind.Words:
                    this.HandleWords();
                    break;
                case DebugConsoleCommandKind.Armoury:
                    this.HandleArmoury();
                    break;
                case DebugConsoleCommandKind.Behold:
                    this.HandleBehold(command.Argument);
                    break;
                case DebugConsoleCommandKind.Scene:
                    this.HandleScene(command.Argument);
                    break;
                case DebugConsoleCommandKind.Entracte:
                    this.HandleEntracte();
                    break;
                case DebugConsoleCommandKind.Finale:
                    this.ResumeAndWait(this.session.Stop);
                    break;
                default:
                    this.WriteColouredLine(ErrorColour, $"Unrecognised command: '{command.Argument}'. Type 'entracte' for a list of commands.");
                    break;
            }
        }
        catch (InvalidOperationException ex)
        {
            this.WriteColouredLine(ErrorColour, ex.Message);
        }
        catch (ArgumentException ex)
        {
            this.WriteColouredLine(ErrorColour, ex.Message);
        }
    }

    /// <summary>
    /// Handles the <c>mark</c> command by setting a breakpoint at the given line.
    /// </summary>
    /// <param name="argument">The line number.</param>
    private void HandleMark(string? argument)
    {
        if (!int.TryParse(argument, out int line))
        {
            this.WriteColouredLine(ErrorColour, "Usage: mark <line>");
            return;
        }

        Breakpoint breakpoint = this.session.SetBreakpoint(line);
        this.WriteColouredLine(BreakpointCreatedColour, breakpoint.IsVerified
            ? $"Breakpoint set at line {line}."
            : $"Line {line} is unreachable; breakpoint will not be hit.");
    }

    /// <summary>
    /// Handles the <c>unmark</c> command by removing the breakpoint at the given line, if any.
    /// </summary>
    /// <param name="argument">The line number.</param>
    private void HandleUnmark(string? argument)
    {
        if (!int.TryParse(argument, out int line))
        {
            this.WriteColouredLine(ErrorColour, "Usage: unmark <line>");
            return;
        }

        foreach (Breakpoint breakpoint in this.session.GetBreakpoints())
        {
            if (breakpoint.Line == line)
            {
                this.session.RemoveBreakpoint(breakpoint.Id);
            }
        }

        this.WriteColouredLine(BreakpointRemovedColour, $"Breakpoint removed at line {line}.");
    }

    /// <summary>
    /// Handles the <c>troupe</c> command by printing the current call stack, marking the selected frame.
    /// </summary>
    private void HandleTroupe()
    {
        int frameNumber = 0;
        foreach (StackFrame frame in this.session.GetCallStack())
        {
            this.WriteLine($"#{frameNumber} {frame.Name} at line {frame.Span.Start.Line}{(frame.Id == this.selectedFrameId ? " (selected)" : string.Empty)}");
            frameNumber++;
        }
    }

    /// <summary>
    /// Handles the <c>cue</c> command by printing the current line and function name of the selected frame.
    /// </summary>
    private void HandleCue()
    {
        StackFrame frame = this.session.GetCallStack().First(frame => frame.Id == this.selectedFrameId);
        this.WriteLine($"Line {frame.Span.Start.Line} in {frame.Name}.");
    }

    /// <summary>
    /// Handles the <c>words</c> command by printing the source text of the current line of the selected frame.
    /// </summary>
    private void HandleWords()
    {
        StackFrame frame = this.session.GetCallStack().First(frame => frame.Id == this.selectedFrameId);
        int line = frame.Span.Start.Line;
        this.WriteLine($"{line}: {this.session.GetSourceLine(line)}");
    }

    /// <summary>
    /// Handles the <c>armoury</c> command by printing every variable in scope in the selected frame.
    /// </summary>
    private void HandleArmoury()
    {
        foreach (VariableScope scope in this.session.GetVariables(this.selectedFrameId))
        {
            this.WriteLine($"{scope.Label}:");
            foreach ((string name, TopsyTurvyValue value) in scope.Variables)
            {
                this.WriteArmouryVariable(name, LiteralTypeNames.ToDisplayName(value.LiteralType), $"{value}");
            }
        }
    }

    /// <summary>
    /// Handles the <c>behold</c> command by evaluating an expression in the selected frame.
    /// </summary>
    /// <param name="argument">The expression.</param>
    private void HandleBehold(string? argument)
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            this.WriteColouredLine(ErrorColour, "Usage: behold <expression>");
            return;
        }

        EvaluationResult result = this.session.Evaluate(argument, this.selectedFrameId);
        if (result.IsSuccess)
        {
            this.WriteLine($"{result.Value}");
        }
        else
        {
            this.WriteColouredLine(ErrorColour, $"Error: {result.ErrorMessage}");
        }
    }

    /// <summary>
    /// Handles the <c>scene</c> command by selecting which stack frame subsequent <c>armoury</c>, <c>behold</c>,
    /// <c>cue</c> or <c>words</c> commands target.
    /// </summary>
    /// <param name="argument">The frame ID.</param>
    private void HandleScene(string? argument)
    {
        if (!int.TryParse(argument, out int frameId))
        {
            this.WriteColouredLine(ErrorColour, "Usage: scene <id>");
            return;
        }

        this.selectedFrameId = frameId;
        this.WriteLine($"Selected frame {frameId}.");
    }

    /// <summary>
    /// Handles the <c>entracte</c> command by printing the list of every debug console command.
    /// </summary>
    private void HandleEntracte()
    {
        this.WriteEntracteLine("mark <line>", "(break, b)", "Set a breakpoint on the given line.");
        this.WriteEntracteLine("unmark <line>", "(clear)", "Remove the breakpoint on the given line.");
        this.WriteEntracteLine("proceed", "(continue, c)", "Resume execution until the next breakpoint or pause.");
        this.WriteEntracteLine("step", "(s)", "Step over the next statement.");
        this.WriteEntracteLine("enter", "(stepin, si)", "Step into the next call.");
        this.WriteEntracteLine("exit", "(stepout, so)", "Step out of the current frame.");
        this.WriteEntracteLine("pause", string.Empty, "Request an on-demand pause.");
        this.WriteEntracteLine("troupe", "(stack)", "Print the call stack.");
        this.WriteEntracteLine("cue", "(line, l)", "Print the current line of the selected frame.");
        this.WriteEntracteLine("words", "(code)", "Print the source text of the current line.");
        this.WriteEntracteLine("armoury", "(vars, v)", "Print the in-scope variables of the selected frame.");
        this.WriteEntracteLine("behold <expr>", "(print, p)", "Evaluate an expression in the selected frame.");
        this.WriteEntracteLine("scene <id>", "(frame, f)", "Select a stack frame by ID.");
        this.WriteEntracteLine("entracte", "(help)", "Print this list of commands.");
        this.WriteEntracteLine("finale", "(quit, q)", "End the debug session.");
    }

    /// <summary>
    /// Invokes a resume-family <see cref="DebugSession"/> member, then blocks the console loop until the next
    /// <see cref="DebugSession.Paused"/> or <see cref="DebugSession.Ended"/> event of the session has been reported.
    /// </summary>
    /// <param name="resume">The <see cref="DebugSession"/> member to call.</param>
    private void ResumeAndWait(Action resume)
    {
        resume();
        this.awaitingNextEvent.Wait();
    }

    /// <summary>
    /// Handles <see cref="DebugSession.Paused"/> by selecting the top frame and reporting the pause to the console.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="eventArgs">The reason and frame the session paused at.</param>
    private void OnPaused(object? sender, PausedEventArgs eventArgs)
    {
        StackFrame frame = this.session.GetCallStack()[0];
        this.selectedFrameId = frame.Id;
        this.WriteColouredLine(PauseColour, $"Paused ({eventArgs.Reason}) at line {frame.Span.Start.Line} in {frame.Name}.");
        this.awaitingNextEvent.Release();
    }

    /// <summary>
    /// Handles <see cref="DebugSession.Output"/> by printing the line the target programme wrote to standard output.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="eventArgs">The line written to standard output.</param>
    private void OnOutput(object? sender, OutputEventArgs eventArgs) => this.WriteLine(eventArgs.Text);

    /// <summary>
    /// Handles <see cref="DebugSession.Ended"/> by reporting the session's completion or error summary, then
    /// releasing the console loop if it is currently blocked waiting on a resume-family command.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="eventArgs">The diagnostics and exit code the session ended with.</param>
    private void OnEnded(object? sender, EndedEventArgs eventArgs)
    {
        this.lastEndedHadErrors = eventArgs.Diagnostics.HasErrors;
        if (eventArgs.Diagnostics.HasErrors)
        {
            PanelHelper.WriteRuntimeErrors(eventArgs.Diagnostics.Diagnostics);
        }
        else if (!this.tiptoe)
        {
            PanelHelper.WriteSuccess("Performance Over!", "[lightgreen_1]And a Good Job Too![/]");
        }

        if (this.awaitingNextEvent.CurrentCount == 0)
        {
            this.awaitingNextEvent.Release();
        }
    }

    /// <summary>
    /// Writes the <c>(director)</c> prompt with no trailing newline.
    /// </summary>
    private void WritePrompt()
    {
        if (this.tiptoe)
        {
            Console.Write("(director) ");
        }
        else
        {
            AnsiConsole.Markup($"[{PromptColour}](director)[/] ");
        }
    }

    /// <summary>
    /// Writes a line to the console.
    /// </summary>
    /// <param name="message">The line to write.</param>
    private void WriteLine(string message)
    {
        if (this.tiptoe)
        {
            Console.WriteLine(message);
        }
        else
        {
            AnsiConsole.MarkupLine(Markup.Escape(message));
        }
    }

    /// <summary>
    /// Writes a line to the console in the given colour.
    /// </summary>
    /// <param name="colour">The Spectre.Console colour name.</param>
    /// <param name="message">The line to write.</param>
    private void WriteColouredLine(string colour, string message)
    {
        if (this.tiptoe)
        {
            Console.WriteLine(message);
        }
        else
        {
            AnsiConsole.MarkupLine($"[{colour}]{Markup.Escape(message)}[/]");
        }
    }

    /// <summary>
    /// Writes one <c>armoury</c> variable line, with its type coloured <see cref="ArmouryTypeColour"/> and its
    /// value coloured.
    /// </summary>
    /// <param name="name">The variable name.</param>
    /// <param name="type">The display name of the variable's type.</param>
    /// <param name="value">The current value, already formatted for display.</param>
    private void WriteArmouryVariable(string name, string type, string value)
    {
        if (this.tiptoe)
        {
            Console.WriteLine($"  {name}: {type} = {value}");
        }
        else
        {
            AnsiConsole.MarkupLine($"  {Markup.Escape(name)}: [{ArmouryTypeColour}]{Markup.Escape(type)}[/] = [{ArmouryValueColour}]{Markup.Escape(value)}[/]");
        }
    }

    /// <summary>
    /// Writes one <c>entracte</c> command list entry, with its command and aliases coloured.
    /// </summary>
    /// <param name="command">The primary command, including any argument placeholder.</param>
    /// <param name="alias">The parenthesised alias list, or <see cref="string.Empty"/> for a command with none.</param>
    /// <param name="description">The description of what the command does.</param>
    private void WriteEntracteLine(string command, string alias, string description)
    {
        if (this.tiptoe)
        {
            Console.WriteLine($"{command,-17}{alias,-17}{description}");
        }
        else
        {
            string paddedCommand = Markup.Escape(command.PadRight(17));
            string paddedAlias = Markup.Escape(alias.PadRight(17));
            AnsiConsole.MarkupLine($"[{EntracteCommandColour}]{paddedCommand}[/][{EntracteAliasColour}]{paddedAlias}[/]{Markup.Escape(description)}");
        }
    }
}
