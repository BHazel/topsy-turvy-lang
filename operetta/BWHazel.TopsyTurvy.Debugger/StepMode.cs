namespace BWHazel.TopsyTurvy.Debugger;

/// <summary>
/// The kind of step a paused <see cref="DebugSession"/> is being resumed under, if any.
/// </summary>
internal enum StepMode
{
    /// <summary>No step is in progress; only breakpoints and manual pause requests can pause execution.</summary>
    None,

    /// <summary>Advance to the next statement in the current frame without descending into a called function.</summary>
    StepOver,

    /// <summary>Advance one statement, descending into a called function if the next statement is a call.</summary>
    StepIn,

    /// <summary>Resume until the current frame returns to its caller, then pause.</summary>
    StepOut
}
