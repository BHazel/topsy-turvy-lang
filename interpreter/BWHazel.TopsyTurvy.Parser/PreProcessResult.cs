namespace BWHazel.TopsyTurvy.Parser;

/// <summary>
/// Carries the output of a single pre-processing pass.
/// </summary>
public record PreProcessResult(string Text, SourceMap SourceMap);
