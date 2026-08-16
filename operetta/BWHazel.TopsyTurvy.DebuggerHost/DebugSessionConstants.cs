namespace BWHazel.TopsyTurvy.DebuggerHost;

/// <summary>
/// Shared constants for mapping a single-threaded <see cref="Debugger.DebugSession"/> onto the inherently
/// multi-threaded protocol shape of DAP.
/// </summary>
internal static class DebugSessionConstants
{
    /// <summary>
    /// The only thread ID ever reported, since Topsy Turvy debugging is single-threaded.
    /// </summary>
    internal const long SingleThreadId = 1;
}
