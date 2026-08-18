namespace BWHazel.TopsyTurvy.Cli.Debug;

/// <summary>
/// Represents a parsed debug console input line.
/// </summary>
/// <param name="Kind">The debugger command kind.</param>
/// <param name="Argument">The remainder of the line after the command, trimmed, or <c>null</c> if there was none.</param>
internal sealed record DebugConsoleCommand(DebugConsoleCommandKind Kind, string? Argument);
