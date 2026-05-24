using System;

namespace BWHazel.TopsyTurvy.Runtime;

/// <summary>
/// Internal signal thrown when a continue statement is executed.
/// </summary>
/// <remarks>
/// This is not a language-visible exception.
/// </remarks>
internal sealed class ContinueSignal : Exception
{
    /// <summary>
    /// Initialises a new instance of the <see cref="ContinueSignal"/> class.
    /// </summary>
    internal ContinueSignal()
        : base()
    {
    }
}
