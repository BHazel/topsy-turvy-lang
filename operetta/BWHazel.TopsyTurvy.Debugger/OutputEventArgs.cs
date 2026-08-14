namespace BWHazel.TopsyTurvy.Debugger;

/// <summary>
/// Provides data for the <see cref="DebugSession.Output"/> event.
/// </summary>
/// <param name="Text">The line printed to standard output.</param>
public sealed record OutputEventArgs(string Text);
