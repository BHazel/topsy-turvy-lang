using System.IO;
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
    /// Executes a Topsy Turvy source file.
    /// </summary>
    /// <param name="filePath">The path to the Topsy Turvy file.</param>
    /// <param name="io">The IO implementation to use during execution.</param>
    /// <returns>A result containing the outcome of the execution.</returns>
    public static ProgramExecutionResult Run(string filePath, ITopsyTurvyIO io)
    {
        if (!File.Exists(filePath))
        {
            return ProgramExecutionResult.Failure($"File not found: {filePath}");
        }

        string source = File.ReadAllText(filePath);
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
}
