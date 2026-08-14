using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Debugger;

/// <summary>
/// Provides data for the <see cref="DebugSession.Ended"/> event.
/// </summary>
/// <param name="Diagnostics">The diagnostics produced by the run.</param>
/// <param name="ExitCode">The OS exit code the target programme produced, valid when <paramref name="Diagnostics"/> has no errors.</param>
public sealed record EndedEventArgs(DiagnosticCollection Diagnostics, int ExitCode);
