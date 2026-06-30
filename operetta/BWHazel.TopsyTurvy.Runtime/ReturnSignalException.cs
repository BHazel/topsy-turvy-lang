using System;

namespace BWHazel.TopsyTurvy.Runtime;

/// <summary>
/// Internal signal thrown when a return statement is executed.
/// </summary>
/// <remarks>
/// This exception is used within the language runtime to signal a <c>MY DUTY IS DISCHARGED.</c> return statement.  It is not an
/// error and is used to cleanly unwind the call stack.  It is not intended to be used or caught by user code.
/// </remarks>
internal sealed class ReturnSignalException : Exception
{
    /// <summary>
    /// Initialises a new instance of the <see cref="ReturnSignalException"/> class with the specified return value.
    /// </summary>
    /// <param name="value">The return value, or <c>null</c> for no return value.</param>
    internal ReturnSignalException(TopsyTurvyValue? value) : base()
    {
        this.Value = value;
    }

    /// <summary>
    /// Gets the return value, or <c>null</c> for no return value.
    /// </summary>
    internal TopsyTurvyValue? Value { get; }
}
