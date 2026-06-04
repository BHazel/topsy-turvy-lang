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
    /// Initializes a new instance of the <see cref="ProgramExecutionResult"/> class.
    /// </summary>
    private ProgramExecutionResult()
    {
    }

    /// <summary>
    /// Creates a successful execution result.
    /// </summary>
    /// <returns>A <see cref="ProgramExecutionResult"/> representing a successful execution.</returns>
    public static ProgramExecutionResult Success() =>
        new()
        {
            IsSuccess = true
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
}
