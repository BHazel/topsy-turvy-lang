using System;
using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.UtopIR.Parser;

/// <summary>
/// Thrown by <see cref="UtopIRParser.Parse(string)"/> when UtopIR source text fails to parse.
/// </summary>
/// <param name="errors">The parse error messages.</param>
public class UtopIRSyntaxException(IEnumerable<string> errors)
    : Exception("Syntax errors occurred while parsing UtopIR source.")
{
    /// <summary>
    /// Gets the parse errors that caused this exception.
    /// </summary>
    public IReadOnlyList<string> Errors { get; } = [.. errors];
}
