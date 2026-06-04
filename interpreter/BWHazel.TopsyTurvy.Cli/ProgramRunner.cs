using System.IO.Abstractions;
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
    /// <param name="fileSystem">The file system to use or <c>null</c> to use the real file system.</param>
    /// <returns>A result containing the outcome of the syntax check.</returns>
    public static ProgramExecutionResult Check(string filePath, IFileSystem? fileSystem = null)
    {
        (ProgramExecutionResult result, _) = ParseFile(filePath, fileSystem);
        return result;
    }

    /// <summary>
    /// Parses a Topsy Turvy source file and returns both the execution result and the raw parse data.
    /// </summary>
    /// <param name="filePath">The path to the Topsy Turvy file.</param>
    /// <param name="fileSystem">The file system to use or <c>null</c> to use the real file system.</param>
    /// <returns>
    /// A tuple containing the <see cref="ProgramExecutionResult"/> and the <see cref="ParseResult"/>,
    /// or <c>null</c> for the parse data if file loading failed before parsing could be attempted.
    /// </returns>
    public static (ProgramExecutionResult Result, ParseResult? ParseData) ParseFile(string filePath, IFileSystem? fileSystem = null)
    {
        (bool success, string? errorMessage) = FileManager.TryReadSource(filePath, out string source, fileSystem);
        if (!success)
        {
            return (ProgramExecutionResult.Failure(errorMessage!), null);
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
    /// <param name="fileSystem">The file system to use or <c>null</c> to use the real file system.</param>
    /// <returns>A result containing the outcome of the execution.</returns>
    public static ProgramExecutionResult Run(string filePath, ITopsyTurvyIO io, IFileSystem? fileSystem = null)
    {
        (bool success, string? errorMessage) = FileManager.TryReadSource(filePath, out string source, fileSystem);
        if (!success)
        {
            return ProgramExecutionResult.Failure(errorMessage!);
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
        DiagnosticCollection diagnostics = interpreter.Execute(program, sourceFilePath: filePath);

        if (diagnostics.HasErrors)
        {
            return ProgramExecutionResult.RuntimeError(diagnostics.Diagnostics);
        }

        return ProgramExecutionResult.Success();
    }
}
