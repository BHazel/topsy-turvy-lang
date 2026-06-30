using System;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Runtime;

/// <summary>
/// Represents a runtime error in the Topsy Turvy runtime.
/// </summary>
/// <remarks>
/// This exception is thrown during execution of a Topsy Turvy programme when a runtime error occurs.  It is not intended to be used
/// in user code.
/// </remarks>
/// <param name="message">A description of the error.</param>
/// <param name="span">The source location of the offending node, if available.</param>
public sealed class TopsyTurvyRuntimeException(string message, SourceSpan? span = null)
    : Exception(message)
{

    /// <summary>
    /// Gets the source location associated with this error, or <c>null</c> if unavailable.
    /// </summary>
    public SourceSpan? Span { get; } = span;
}
