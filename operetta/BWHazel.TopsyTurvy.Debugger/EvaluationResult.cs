using BWHazel.TopsyTurvy.Runtime;

namespace BWHazel.TopsyTurvy.Debugger;

/// <summary>
/// The outcome of evaluating an expression against a paused <see cref="DebugSession"/> frame.
/// </summary>
/// <param name="IsSuccess"><c>true</c> when the expression parsed and evaluated without error, otherwise <c>false</c>.</param>
/// <param name="Value">The evaluated value, when <paramref name="IsSuccess"/> is <c>true</c>, otherwise <c>null</c>.</param>
/// <param name="ErrorMessage">A description of the parse or evaluation failure, when <paramref name="IsSuccess"/> is <c>false</c>, otherwise <c>null</c>.</param>
/// <remarks>
/// A failed evaluation is reported through this result, not thrown, since a mistyped watch expression must not
/// crash the paused session.
/// </remarks>
public sealed record EvaluationResult(bool IsSuccess, TopsyTurvyValue? Value, string? ErrorMessage)
{
    /// <summary>
    /// Creates a successful <see cref="EvaluationResult"/>.
    /// </summary>
    /// <param name="value">The evaluated value.</param>
    /// <returns>A successful result carrying <paramref name="value"/>.</returns>
    public static EvaluationResult Ok(TopsyTurvyValue value) => new(true, value, null);

    /// <summary>
    /// Creates a failed <see cref="EvaluationResult"/>.
    /// </summary>
    /// <param name="errorMessage">A description of the failure.</param>
    /// <returns>A failed result carrying <paramref name="errorMessage"/>.</returns>
    public static EvaluationResult Failed(string errorMessage) => new(false, null, errorMessage);
}
