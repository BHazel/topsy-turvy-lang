namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a specific location in the source code.
/// </summary>
/// <param name="Line">The 1-indexed line number.</param>
/// <param name="Column">The 1-indexed column number.</param>
public record TopsyTurvySourceLocation(int Line, int Column);
