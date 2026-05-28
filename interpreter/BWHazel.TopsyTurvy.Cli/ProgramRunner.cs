using System.IO;
using System.Linq;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Parser;
using BWHazel.TopsyTurvy.Runtime;

namespace BWHazel.TopsyTurvy.Cli;

/// <summary>
/// Orchestrates the loading, parsing, and execution of a Topsy Turvy program.
/// </summary>
public class ProgramRunner
{
    /// <summary>
    /// Checks a Topsy Turvy source file for syntax errors without executing it.
    /// </summary>
    /// <param name="filePath">The path to the Topsy Turvy file.</param>
    /// <returns>A result containing the outcome of the syntax check.</returns>
    public static ProgramExecutionResult Check(string filePath)
    {
        (ProgramExecutionResult result, _) = ParseFile(filePath);
        return result;
    }

    /// <summary>
    /// Parses a Topsy Turvy source file and returns both the execution result and the raw parse data.
    /// </summary>
    /// <param name="filePath">The path to the Topsy Turvy file.</param>
    /// <returns>
    /// A tuple containing the <see cref="ProgramExecutionResult"/> and the <see cref="ParseResult"/>,
    /// or <c>null</c> for the parse data if file loading failed before parsing could be attempted.
    /// </returns>
    public static (ProgramExecutionResult Result, ParseResult? ParseData) ParseFile(string filePath)
    {
        if (!TryReadSource(filePath, out string source, out ProgramExecutionResult? failure))
        {
            return (failure!, null);
        }

        TopsyTurvyParser parser = new();
        ParseResult parseData = parser.TryParse(source);

        if (!parseData.Success)
        {
            return (ProgramExecutionResult.SyntaxError(
                parseData.Diagnostics.Select(d => $"[{d.Span.Start.Line}:{d.Span.Start.Column}] {d.Message}")),
                parseData);
        }

        return (ProgramExecutionResult.Success(), parseData);
    }

    /// <summary>
    /// Executes a Topsy Turvy source file.
    /// </summary>
    /// <param name="filePath">The path to the Topsy Turvy file.</param>
    /// <param name="io">The IO implementation to use during execution.</param>
    /// <returns>A result containing the outcome of the execution.</returns>
    public static ProgramExecutionResult Run(string filePath, ITopsyTurvyIO io)
    {
        if (!TryReadSource(filePath, out string source, out ProgramExecutionResult? failure))
        {
            return failure!;
        }

        TopsyTurvyParser parser = new();
        ProgramNode program;

        try
        {
            program = parser.Parse(source);
        }
        catch (TopsyTurvySyntaxException ex)
        {
            return ProgramExecutionResult.SyntaxError(ex.Errors);
        }

        Interpreter interpreter = new(io);
        DiagnosticCollection diagnostics = interpreter.Execute(program);

        if (diagnostics.HasErrors)
        {
            return ProgramExecutionResult.RuntimeError(diagnostics.Diagnostics);
        }

        return ProgramExecutionResult.Success();
    }

    /// <summary>
    /// Attempts to read the source text of a Topsy Turvy file.
    /// </summary>
    /// <param name="filePath">The path to the Topsy Turvy file.</param>
    /// <param name="source">The file contents when successful, otherwise <see cref="string.Empty"/>.</param>
    /// <param name="failure">A failure result when reading fails, <c>null</c> on success.</param>
    /// <returns><c>true</c> if the file was read successfully, otherwise <c>false</c>.</returns>
    private static bool TryReadSource(string filePath, out string source, out ProgramExecutionResult? failure)
    {
        source = string.Empty;
        if (!File.Exists(filePath))
        {
            failure = ProgramExecutionResult.Failure($"File not found: {filePath}");
            return false;
        }

        try
        {
            source = File.ReadAllText(filePath);
            failure = null;
            return true;
        }
        catch (IOException ex)
        {
            failure = ProgramExecutionResult.Failure($"Could not read '{filePath}': {ex.Message}");
            return false;
        }
    }
}
