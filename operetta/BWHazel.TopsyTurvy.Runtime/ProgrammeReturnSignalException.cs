using System;

namespace BWHazel.TopsyTurvy.Runtime;

/// <summary>
/// Internal signal thrown when a top-level programme return statement is executed.
/// </summary>
/// <remarks>
/// This exception is used within the language runtime to signal an <c>AND SO I FIND</c> statement at the top
/// level of a programme body.  It is not an error and it cleanly unwinds the call stack so that
/// <see cref="Interpreter.Execute"/> can capture the exit code and store it in
/// <see cref="Interpreter.ExitCode"/> before returning to the caller.  It is not intended to be caught
/// anywhere other than the top-level <c>Execute</c> method.
/// </remarks>
internal sealed class ProgrammeReturnSignalException : Exception
{
    /// <summary>
    /// Initialises a new instance of the <see cref="ProgrammeReturnSignalException"/> class with the specified exit code.
    /// </summary>
    /// <param name="exitCode">The OS exit code returned by the programme.</param>
    internal ProgrammeReturnSignalException(int exitCode) : base()
    {
        this.ExitCode = exitCode;
    }

    /// <summary>
    /// Gets the OS exit code returned by the programme.
    /// </summary>
    internal int ExitCode { get; }
}
