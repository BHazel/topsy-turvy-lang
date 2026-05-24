using System;

namespace BWHazel.TopsyTurvy.Runtime;

/// <summary>
/// Internal signal thrown when a break statement is executed.
/// </summary>
/// <remarks>
/// This is not a language-visible exception.
/// </remarks>
internal sealed class BreakSignalException : Exception
{
    /// <summary>
    /// Initialises a new instance of the <see cref="BreakSignalException"/> class.
    /// </summary>
    internal BreakSignalException()
        : base()
    {
    }
}
