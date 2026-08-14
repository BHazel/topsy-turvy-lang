namespace BWHazel.TopsyTurvy.Debugger;

/// <summary>
/// Defines constants for the current lifecycle status of a <see cref="DebugSession"/>.
/// </summary>
public enum SessionStatus
{
    /// <summary>The session exists but <see cref="DebugSession.StartAsync"/> has not yet been called.</summary>
    NotStarted,

    /// <summary>The target programme is executing.</summary>
    Running,

    /// <summary>Execution is paused at a breakpoint, a completed step, an unhandled error or a manual pause.</summary>
    Paused,

    /// <summary>The session has ended either because the programme finished or <see cref="DebugSession.Stop"/> was called.</summary>
    Ended
}
