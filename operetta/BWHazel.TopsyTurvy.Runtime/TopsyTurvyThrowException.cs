using System;

namespace BWHazel.TopsyTurvy.Runtime;

/// <summary>
/// Represents a language-level exception thrown within a Topsy Turvy programme, raised by its throw construct.
/// </summary>
/// <remarks>
/// This exception is thrown during execution of a Topsy Turvy programme as raised by the <c>A HIDEOUS CURSE ON</c> throw construct.
/// It is not a runtime fault but a language-visible construct.  Additionally, it is not intended to be used or caught by user code
/// but caught by the runtime itself within a <c>WITH THE GREATEST RESPECT</c> try-catch block.
/// </remarks>
/// <param name="throwValue">The value carried as the exception payload.</param>
public sealed class TopsyTurvyThrowException(TopsyTurvyValue throwValue)
    : Exception(throwValue.ToString())
{

    /// <summary>
    /// Gets the value carried as the exception payload.
    /// </summary>
    public TopsyTurvyValue ThrowValue { get; } = throwValue;
}
