using System;

namespace BWHazel.TopsyTurvy.Runtime;

/// <summary>
/// Internal signal thrown when a continue statement is executed.
/// </summary>
/// <remarks>
/// This exception is used within the language runtime to signal a <c>ONCE MORE.</c> continue statement.  It is not an error and is
/// used to cleanly unwind the call stack.  It is not intended to be used or caught by user code.
/// </remarks>
internal sealed class ContinueSignalException : Exception
{
    /// <summary>
    /// Initialises a new instance of the <see cref="ContinueSignalException"/> class.
    /// </summary>
    internal ContinueSignalException()
        : base()
    {
    }
}
