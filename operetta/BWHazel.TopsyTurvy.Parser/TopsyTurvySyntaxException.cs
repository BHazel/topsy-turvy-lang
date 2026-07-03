using System;
using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.Parser;

/// <summary>
/// Exception raised when the source text contains syntax errors.
/// </summary>
/// <remarks>
/// This exceptions is typically thrown during paring of a Topsy Turvy programme when syntax errors occur, although it should be noted
/// it is also used on semantic errors.  It is not intended to be used in user code.
/// </remarks>
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
