namespace BWHazel.TopsyTurvy.Debugger;

/// <summary>
/// Provides data for the <see cref="DebugSession.Paused"/> event.
/// </summary>
/// <param name="Reason">The reason why execution paused.</param>
/// <param name="FrameId">The ID of the stack frame execution paused in.</param>
public sealed record PausedEventArgs(PauseReason Reason, int FrameId);
