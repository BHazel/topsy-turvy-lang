using System;

namespace BWHazel.TopsyTurvy.Runtime;

/// <summary>
/// Represents a language-level exception raised by its throw construct.
/// </summary>
/// <remarks>
/// This is a language-visible construct, not an interpreter fault.
/// </remarks>
public sealed class TopsyTurvyThrowException : Exception
{
    /// <summary>
    /// Initialises a new instance of the <see cref="TopsyTurvyThrowException"/> class carrying the specified value to throw.
    /// </summary>
    /// <param name="throwValue">The value carried as the exception payload.</param>
    public TopsyTurvyThrowException(TopsyTurvyValue throwValue)
        : base(throwValue.ToString())
    {
        this.ThrowValue = throwValue;
    }

    /// <summary>
    /// Gets the value carried as the exception payload.
    /// </summary>
    public TopsyTurvyValue ThrowValue { get; }
}
