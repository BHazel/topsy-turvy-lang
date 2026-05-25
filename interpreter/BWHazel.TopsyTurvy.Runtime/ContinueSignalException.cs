using System;

namespace BWHazel.TopsyTurvy.Runtime;

/// <summary>
/// Internal signal thrown when a continue statement is executed.
/// </summary>
/// <remarks>
/// This is not a language-visible exception.
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
