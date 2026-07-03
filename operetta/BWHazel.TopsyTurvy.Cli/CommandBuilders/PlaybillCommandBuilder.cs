using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using BWHazel.TopsyTurvy.Analysis;
using BWHazel.TopsyTurvy.Parser;
using Spectre.Console;

using TopsyParseResult = BWHazel.TopsyTurvy.Parser.ParseResult;

namespace BWHazel.TopsyTurvy.Cli.CommandBuilders;

/// <summary>
/// Builder for the <c>playbill</c> command.
/// </summary>
public static class PlaybillCommandBuilder
{
    /// <summary>
    /// Builds and configures the <c>playbill</c> command.
    /// </summary>
    /// <returns>A configured <see cref="Command"/> instance.</returns>
    public static Command Build()
    {
        Command playbillCommand = new("playbill", "Generates Markdown documentation for a Topsy Turvy .topsy file and outputs the result.");
        playbillCommand.Aliases.Add("docs");

        Argument<string> fileArgument = new("filename")
        {
            Description = "The Topsy Turvy file to document."
        };

        Option<bool> chromaticOption = new("--chromatic")
        {
            Description = "Applies syntax highlighting to the Markdown output using Spectre.Console markup."
        };

        chromaticOption.Aliases.Add("-c");

        Option<bool> tiptoeOption = new("--tiptoe")
        {
            Description = "Only prints the documentation output and thus chooses to discard aestheticism."
        };

        playbillCommand.Arguments.Add(fileArgument);
        playbillCommand.Options.Add(chromaticOption);
        playbillCommand.Options.Add(tiptoeOption);

        playbillCommand.SetAction(parseResult =>
            HandlePlaybill(
                parseResult.GetValue(fileArgument) ?? string.Empty,
                parseResult.GetValue(chromaticOption),
                parseResult.GetValue(tiptoeOption)));

        return playbillCommand;
    }

    /// <summary>
    /// Handles the execution of the <c>playbill</c> command.
    /// </summary>
    /// <param name="filename">The filename of the Topsy Turvy file to document.</param>
    /// <param name="chromatic">A value indicating whether to apply syntax highlighting to the output.</param>
    /// <param name="tiptoe">A value indicating whether to only print the documentation output.</param>
    /// <returns>An integer exit code with 0 for success or 1 for failure.</returns>
    private static int HandlePlaybill(string filename, bool chromatic, bool tiptoe)
    {
        if (!tiptoe)
        {
            PanelHelper.WriteDefault("Reading the Playbill...", $"[cyan]{filename}[/]");
        }

        (bool success, string? errorMessage) = FileManager.TryReadSource(filename, out string source);
        if (!success)
        {
            PanelHelper.ReportErrors(ProgramExecutionResult.Failure(errorMessage!), tiptoe);
            return 1;
        }

        TopsyTurvyParser parser = new();
        TopsyParseResult parseResult = parser.TryParse(source);
        if (!parseResult.Success)
        {
            PanelHelper.ReportErrors(
                ProgramExecutionResult.SyntaxError(
                    parseResult.Diagnostics.Select(diagnostic => $"[{diagnostic.Span.Start.Line}:{diagnostic.Span.Start.Column}] {diagnostic.Message}")),
                tiptoe);

            return 1;
        }

        SymbolTable symbolTable = SymbolTable.Build(parseResult.Program!, source);
        WriteMarkdown($"# {Path.GetFileName(filename)}\n", chromatic);
        foreach (SymbolInfo symbol in symbolTable.AllSymbols().Where(symbol => symbol.Documentation != null))
        {
            string markdown = $"## {symbol.Name}\n\n{HoverMarkdownBuilder.Build(symbol)}\n";
            WriteMarkdown(markdown, chromatic);
        }

        return 0;
    }

    /// <summary>
    /// Writes a Markdown string to the console, with optional chromatic markup.
    /// </summary>
    /// <param name="markdown">The Markdown string to write.</param>
    /// <param name="chromatic">A value indicating whether to apply chromatic Markup highlighting.</param>
    private static void WriteMarkdown(string markdown, bool chromatic)
    {
        if (chromatic)
        {
            AnsiConsole.Markup(ApplyChromaticMarkup(markdown));
        }
        else
        {
            Console.WriteLine(markdown);
        }
    }

    /// <summary>
    /// Converts a Markdown string to a Spectre.Console markup string with syntax highlighting.
    /// </summary>
    /// <param name="markdown">The Markdown string to convert.</param>
    /// <returns>A Spectre.Console markup string with titles, bold, italic and code highlighted.</returns>
    /// <remarks>
    /// Processes the text line-by-line.  Code fences are stripped, and content between them is rendered the same.
    /// Section headers such as <c>**Parameters:**</c> and <c>**Throws:**</c> are tracked so that bullet-list
    /// entries in those sections receive context-appropriate colours. All user content is escaped with
    /// <see cref="Markup.Escape"/> before markup tags are applied.
    /// </remarks>
    private static string ApplyChromaticMarkup(string markdown)
    {
        string[] lines = markdown.Split('\n');
        StringBuilder output = new();
        bool inCodeBlock = false;
        string currentSection = string.Empty;

        foreach (string line in lines)
        {
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal))
            {
                inCodeBlock = !inCodeBlock;
                continue;
            }

            if (inCodeBlock)
            {
                output.AppendLine($"[grey]{Markup.Escape(line)}[/]");
                continue;
            }

            string escapedLine = Markup.Escape(line);

            if (escapedLine == "---")
            {
                currentSection = string.Empty;
                output.AppendLine("[grey]────────────────────────────────────────[/]");
                continue;
            }

            if (escapedLine.StartsWith("## ", StringComparison.Ordinal))
            {
                currentSection = string.Empty;
                output.AppendLine($"[bold deepskyblue1]{ApplyInlineMarkup(escapedLine[3..])}[/]");
                continue;
            }

            if (escapedLine.StartsWith("# ", StringComparison.Ordinal))
            {
                currentSection = string.Empty;
                output.AppendLine($"[bold lightgreen_1]{ApplyInlineMarkup(escapedLine[2..])}[/]");
                continue;
            }

            // Deprecated notice emitted by HoverMarkdownBuilder as ~~Deprecated~~ or ~~Deprecated~~ : message.
            if (escapedLine.StartsWith("~~Deprecated~~", StringComparison.Ordinal))
            {
                string message = escapedLine["~~Deprecated~~".Length..].TrimStart(' ', ':').Trim();
                output.AppendLine(string.IsNullOrEmpty(message)
                    ? "[yellow]Deprecated[/]"
                    : $"[yellow]Deprecated:[/] [yellow]{message}[/]");
                continue;
            }

            Match sectionHeader = Regex.Match(escapedLine, @"^\*\*([A-Za-z]+):\*\*");
            if (sectionHeader.Success)
            {
                currentSection = sectionHeader.Groups[1].Value.ToUpperInvariant();
            }

            output.AppendLine(ApplySectionMarkup(escapedLine, currentSection));
        }

        return output.ToString();
    }

    /// <summary>
    /// Applies context-sensitive Spectre.Console markup to a single escaped line based on the current section.
    /// </summary>
    /// <param name="escapedLine">A Spectre.Console-escaped line of text.</param>
    /// <param name="section">The uppercased name of the current documentation section, e.g. <c>PARAMETERS</c>.</param>
    /// <returns>The line with section-aware markup applied.</returns>
    /// <remarks>
    /// Bullet-list entries of the form <c>- `name` (`type`): description</c> receive colours determined by
    /// <paramref name="section"/>. All other lines fall through to <see cref="ApplyInlineMarkup"/>.
    /// </remarks>
    private static string ApplySectionMarkup(string escapedLine, string section)
    {
        // Variable/constant signature: **(variable)** `name` : TYPE
        Match varSignatureMatch = Regex.Match(escapedLine, @"^\*\*\(((?:variable|constant))\)\*\* `([^`]+)` : (.+)$");
        if (varSignatureMatch.Success)
        {
            string kind = varSignatureMatch.Groups[1].Value;
            string name = varSignatureMatch.Groups[2].Value;
            string type = varSignatureMatch.Groups[3].Value;
            return $"[bold]({kind})[/] [cyan]{name}[/] : [purple]{type}[/]";
        }

        // Function signature: **(function)** `name`(param1, param2)
        Match funcSignatureMatch = Regex.Match(escapedLine, @"^\*\*\(function\)\*\* `([^`]+)`\(([^)]*)\)$");
        if (funcSignatureMatch.Success)
        {
            string name = funcSignatureMatch.Groups[1].Value;
            string parametersString = funcSignatureMatch.Groups[2].Value;
            string colouredParameters = string.IsNullOrWhiteSpace(parametersString)
                ? string.Empty
                : string.Join(", ", parametersString.Split(", ").Select(p => $"[orange1]{p.Trim()}[/]"));
            return $"[bold](function)[/] [cyan]{name}[/]({colouredParameters})";
        }

        // Bullet list items: - `name` (`type`): description
        Match bulletMatch = Regex.Match(escapedLine, @"^(- )`([^`]+)` \(`([^`]+)`\): (.+)$");
        if (bulletMatch.Success)
        {
            string prefix = bulletMatch.Groups[1].Value;
            string name = bulletMatch.Groups[2].Value;
            string type = bulletMatch.Groups[3].Value;
            string description = bulletMatch.Groups[4].Value;
            string nameColour = section switch
            {
                "PARAMETERS" => "orange1",
                "THROWS" => "red",
                _ => "cyan"
            };
            return $"{prefix}[{nameColour}]{name}[/] ([purple]{type}[/]): {description}";
        }

        // Returns line: **Returns:** (`type`) description
        Match returnsMatch = Regex.Match(escapedLine, @"^\*\*Returns:\*\* \(`([^`]+)`\) (.+)$");
        if (returnsMatch.Success)
        {
            string type = returnsMatch.Groups[1].Value;
            string description = returnsMatch.Groups[2].Value;
            return $"[bold]Returns:[/] ([purple]{type}[/]) {description}";
        }

        return ApplyInlineMarkup(escapedLine);
    }

    /// <summary>
    /// Applies inline Spectre.Console markup for bold, italic, inline code and strikethrough patterns.
    /// </summary>
    /// <param name="escapedLine">A Spectre.Console-escaped line of text.</param>
    /// <returns>The line with inline markup applied.</returns>
    private static string ApplyInlineMarkup(string escapedLine)
    {
        string result = Regex.Replace(escapedLine, @"\*\*(.+?)\*\*", "[bold]$1[/]");
        result = Regex.Replace(result, @"(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)", "[italic]$1[/]");
        result = Regex.Replace(result, @"`(.+?)`", "[cyan]$1[/]");
        result = Regex.Replace(result, @"~~(.+?)~~", "[dim]$1[/]");
        return result;
    }
}
