using System;
using System.Collections.Generic;
using System.Linq;
using BWHazel.TopsyTurvy.Ast;
using Spectre.Console;

namespace BWHazel.TopsyTurvy.Cli;

/// <summary>
/// Creates and writes standard Spectre.Console panels used in the Topsy Turvy CLI.
/// </summary>
public static class PanelHelper
{
    private static readonly Color DefaultBorderColour = Color.Cyan;
    private static readonly Color SuccessBorderColour = Color.LightGreen_1;
    private static readonly Color ErrorBorderColour = Color.Red;

    private static readonly string WarningColour = "lightgoldenrod2_2";
    private static readonly string TableRowValueColour = "lightgreen_1";

    /// <summary>
    /// Writes a default information panel to the console.
    /// </summary>
    /// <param name="header">The panel header text.</param>
    /// <param name="body">The panel body as a Spectre.Console markup string.</param>
    public static void WriteDefault(string header, string body)
    {
        Panel panel = new(body)
        {
            BorderStyle = new Style(DefaultBorderColour),
            Header = new PanelHeader(header)
        };

        AnsiConsole.Write(panel);
    }

    /// <summary>
    /// Writes a success panel to the console.
    /// </summary>
    /// <param name="header">The panel header text.</param>
    /// <param name="body">The panel body as a Spectre.Console markup string.</param>
    public static void WriteSuccess(string header, string body)
    {
        Panel panel = new(body)
        {
            BorderStyle = new Style(SuccessBorderColour),
            Header = new PanelHeader(header)
        };

        AnsiConsole.Write(panel);
    }

    /// <summary>
    /// Writes an error panel to the console for user errors.
    /// </summary>
    /// <param name="message">The plain-text error message to display.</param>
    public static void WriteUserError(string message)
    {
        Panel panel = new(new Markup($"[red]Error: {Markup.Escape(message)}[/]"))
        {
            BorderStyle = new Style(ErrorBorderColour),
            Header = new PanelHeader("Why, Damme!")
        };

        AnsiConsole.Write(panel);
    }

    /// <summary>
    /// Writes an error panel to the console for syntax errors.
    /// </summary>
    /// <param name="errors">The collection of plain-text syntax error messages.</param>
    public static void WriteSyntaxErrors(IEnumerable<string> errors)
    {
        string escapedErrors = Markup.Escape(string.Join("\n", errors));
        Panel panel = new(new Markup($"[red]Syntax Error:\n{escapedErrors}[/]"))
        {
            BorderStyle = new Style(ErrorBorderColour),
            Header = new PanelHeader("Crushed Again!")
        };

        AnsiConsole.Write(panel);
    }

    /// <summary>
    /// Writes an error panel to the console for runtime errors.
    /// </summary>
    /// <param name="diagnostics">The collection of runtime diagnostics to display.</param>
    public static void WriteRuntimeErrors(IEnumerable<Diagnostic> diagnostics)
    {
        string body = string.Join("\n", diagnostics.Select(
            diagnostic => $"[[{diagnostic.Span.Start.Line}:{diagnostic.Span.Start.Column}]] {Markup.Escape(diagnostic.Message)}"));

        WriteRuntimeErrorPanel(body);
    }

    /// <summary>
    /// Writes an error panel to the console for a runtime error as a plain-text message.
    /// </summary>
    /// <param name="message">The plain-text runtime error message.</param>
    public static void WriteRuntimeErrors(string message)
    {
        WriteRuntimeErrorPanel(Markup.Escape(message));
    }

    /// <summary>
    /// Writes a runtime error panel to the console with the given pre-escaped body text.
    /// </summary>
    /// <param name="escapedBody">The pre-escaped body text.</param>
    private static void WriteRuntimeErrorPanel(string escapedBody)
    {
        Panel panel = new(new Markup($"[red]Runtime Error:\n{escapedBody}[/]"))
        {
            BorderStyle = new Style(ErrorBorderColour),
            Header = new PanelHeader("A Hideous Curse!")
        };

        AnsiConsole.Write(panel);
    }

    /// <summary>
    /// Writes a panel displaying the version information of the Topsy Turvy CLI.
    /// </summary>
    /// <param name="version">The CLI version.</param>
    /// <param name="commitHash">The commit SHA hash.</param>
    /// <param name="specVersion">The supported language spec version.</param>
    public static void WriteVersionInfo(string version, string commitHash, string specVersion)
    {
        Table table = new()
        {
            Border = TableBorder.None,
            ShowHeaders = false
        };

        table.AddColumn(new TableColumn(string.Empty));
        table.AddColumn(new TableColumn(string.Empty));

        table.AddRow("CLI Version", $"[{TableRowValueColour}]{Markup.Escape(version)}[/]");
        table.AddRow("Commit SHA", $"[{TableRowValueColour}]{Markup.Escape(commitHash)}[/]");
        table.AddRow("Language Spec", $"[{TableRowValueColour}]{Markup.Escape(specVersion)}[/]");

        Rows content = new(
            new Markup($"[{WarningColour}]I’ve information vegetable, animal, and mineral:[/]"),
            table);

        Panel panel = new(content)
        {
            BorderStyle = new Style(DefaultBorderColour),
            Header = new PanelHeader("The Topsy Turvy Programming Language")
        };

        AnsiConsole.Write(panel);
    }

    /// <summary>
    /// Reports errors from a <see cref="ProgramExecutionResult"/> to the console.
    /// </summary>
    /// <param name="result">The execution result containing the errors to report.</param>
    /// <param name="isPlainText">A value indicating whether to report errors as plain text or as styled panels.</param>
    /// <remarks>
    /// Errors are reported using either plain-text output or rich Spectre.Console panels.
    /// </remarks>
    public static void ReportErrors(ProgramExecutionResult result, bool isPlainText)
    {
        if (result.ErrorMessage != null)
        {
            if (isPlainText)
            {
                Console.Error.WriteLine(result.ErrorMessage);
            }
            else
            {
                WriteUserError(result.ErrorMessage);
            }
        }

        if (result.SyntaxErrors != null)
        {
            if (isPlainText)
            {
                foreach (string error in result.SyntaxErrors)
                {
                    Console.Error.WriteLine(error);
                }
            }
            else
            {
                WriteSyntaxErrors(result.SyntaxErrors);
            }
        }

        if (result.RuntimeDiagnostics != null)
        {
            if (isPlainText)
            {
                foreach (Diagnostic diagnostic in result.RuntimeDiagnostics)
                {
                    Console.Error.WriteLine($"[{diagnostic.Span.Start.Line}:{diagnostic.Span.Start.Column}] {diagnostic.Message}");
                }
            }
            else
            {
                WriteRuntimeErrors(result.RuntimeDiagnostics);
            }
        }
    }
}
