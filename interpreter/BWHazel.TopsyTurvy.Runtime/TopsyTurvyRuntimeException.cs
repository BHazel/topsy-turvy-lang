using System;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Runtime;

/// <summary>
/// Represents a runtime error in the Topsy Turvy runtime.
/// </summary>
public sealed class TopsyTurvyRuntimeException : Exception
{
    /// <summary>
    /// Initialises a new instance of the <see cref="TopsyTurvyRuntimeException"/> class with the specified message and optional source span.
    /// </summary>
    /// <param name="message">A description of the error.</param>
    /// <param name="span">The source location of the offending node, if available.</param>
    public TopsyTurvyRuntimeException(string message, SourceSpan? span = null)
        : base(message)
    {
        this.Span = span;
    }

    /// <summary>
    /// Gets the source location associated with this error, or <c>null</c> if unavailable.
    /// </summary>
    public SourceSpan? Span { get; }
}
