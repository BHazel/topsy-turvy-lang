namespace BWHazel.TopsyTurvy.Debugger;

/// <summary>
/// Defines constants for why a <see cref="DebugSession"/> paused.
/// </summary>
public enum PauseReason
{
    /// <summary>A breakpoint was hit.</summary>
    BreakpointHit,

    /// <summary>A requested step completed.</summary>
    StepComplete,

    /// <summary>An unhandled runtime error was raised.</summary>
    UnhandledError,

    /// <summary>The developer requested an on-demand pause.</summary>
    ManualPause
}
