using System;
using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Parser;

/// <summary>
/// Exception raised when the source text contains syntax errors.
/// </summary>
public class TopsyTurvySyntaxException : Exception
{
    /// <summary>
    /// Gets the error messages describing each syntax problem.
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// Initialises a new instance of <see cref="TopsyTurvySyntaxException"/> with the supplied errors.
    /// </summary>
    /// <param name="errors">The collection of error message strings.</param>
    public TopsyTurvySyntaxException(IEnumerable<string> errors)
        : base("Syntax errors occurred during parsing.")
    {
        this.Errors = [.. errors];
    }
}
