using System.Collections.Generic;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Cli;

/// <summary>
/// Represents the result of a program execution attempt.
/// </summary>
public class ProgramExecutionResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the execution was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the OS exit code produced by the programme, valid only when <see cref="IsSuccess"/> is <c>true</c>.
    /// </summary>
    public int ExitCode { get; set; }

    /// <summary>
    /// Gets or sets an error message if the execution failed due to a general error.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets a list of syntax errors if the execution failed due to syntax issues.
    /// </summary>
    public IReadOnlyList<string>? SyntaxErrors { get; set; }

    /// <summary>
    /// Gets or sets a list of runtime diagnostics if the execution failed due to runtime errors.
    /// </summary>
    public IReadOnlyList<Diagnostic>? RuntimeDiagnostics { get; set; }

    /// <summary>
    /// Gets or sets a list of type-check diagnostics when execution failed due to type errors.
    /// </summary>
    public IReadOnlyList<Diagnostic>? TypeDiagnostics { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ProgramExecutionResult"/> class.
    /// </summary>
    private ProgramExecutionResult()
    {
    }

    /// <summary>
    /// Creates a successful execution result with the programme OS exit code.
    /// </summary>
    /// <param name="exitCode">The OS exit code returned by the programme (0 if not set).</param>
    /// <returns>A <see cref="ProgramExecutionResult"/> representing a successful execution.</returns>
    public static ProgramExecutionResult Success(int exitCode = 0) =>
        new()
        {
            IsSuccess = true,
            ExitCode = exitCode
        };

    /// <summary>
    /// Creates a failed execution result with a general error message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>A <see cref="ProgramExecutionResult"/> representing a failed execution.</returns>
    public static ProgramExecutionResult Failure(string message) =>
        new()
        {
            IsSuccess = false,
            ErrorMessage = message
        };

    /// <summary>
    /// Creates a failed execution result due to syntax errors.
    /// </summary>
    /// <param name="errors">The list of syntax errors.</param>
    /// <returns>A <see cref="ProgramExecutionResult"/> representing a failed execution due to syntax errors.</returns>
    public static ProgramExecutionResult SyntaxError(IEnumerable<string> errors) =>
        new()
        {
            IsSuccess = false,
            SyntaxErrors = new List<string>(errors).AsReadOnly()
        };

    /// <summary>
    /// Creates a failed execution result due to runtime errors.
    /// </summary>
    /// <param name="diagnostics">The list of runtime diagnostics.</param>
    /// <returns>A <see cref="ProgramExecutionResult"/> representing a failed execution due to runtime errors.</returns>
    public static ProgramExecutionResult RuntimeError(IEnumerable<Diagnostic> diagnostics) =>
        new()
        {
            IsSuccess = false,
            RuntimeDiagnostics = new List<Diagnostic>(diagnostics).AsReadOnly()
        };

    /// <summary>
    /// Creates a failed execution result due to type errors.
    /// </summary>
    /// <param name="diagnostics">The type-check diagnostics.</param>
    /// <returns>A <see cref="ProgramExecutionResult"/> representing a failed execution due to type errors.</returns>
    public static ProgramExecutionResult TypeErrors(IReadOnlyList<Diagnostic> diagnostics) =>
        new()
        {
            IsSuccess = false,
            TypeDiagnostics = diagnostics
        };
}
