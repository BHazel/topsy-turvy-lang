using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Debugger;

/// <summary>
/// A developer-set marker on a specific source line that causes a debug session to pause before that line executes.
/// </summary>
/// <param name="Id">The identity of the breakpoint, unchanged for its lifetime.</param>
/// <param name="FilePath">The source file of the session.</param>
/// <param name="Line">The 1-based source line, matching the line numbering of <see cref="SourceSpan"/>.</param>
/// <param name="IsVerified"><c>true</c> when a statement in the parsed programme starts on <see cref="Line"/> so the breakpoint can actually be hit, otherwise <c>false</c>.</param>
public sealed record Breakpoint(int Id, string FilePath, int Line, bool IsVerified);
