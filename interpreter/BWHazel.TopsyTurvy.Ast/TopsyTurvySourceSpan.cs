namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a range of source code between a start and end location.
/// </summary>
/// <param name="Start">The starting location of the span.</param>
/// <param name="End">The ending location of the span.</param>
public record TopsyTurvySourceSpan(TopsyTurvySourceLocation Start, TopsyTurvySourceLocation End);
