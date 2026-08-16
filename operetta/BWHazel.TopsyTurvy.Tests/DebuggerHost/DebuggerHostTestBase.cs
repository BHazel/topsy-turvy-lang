using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Debugger;
using BWHazel.TopsyTurvy.Tests.Debugger;

namespace BWHazel.TopsyTurvy.Tests.DebuggerHost;

/// <summary>
/// Base class for DAP handler tests.
/// </summary>
public abstract class DebuggerHostTestBase
{
    /// <summary>
    /// Starts a session on a file and runs it until it pauses at the given breakpoint line.
    /// </summary>
    /// <param name="breakpointLine">The 1-based line to break at.</param>
    /// <returns>The paused session.</returns>
    protected static async Task<DebugSession> CreatePausedSessionAsync(int breakpointLine)
    {
        DebugSession session = new(TestDebugPrograms.Resolver);
        await session.StartAsync(TestDebugPrograms.FixtureFileName, CancellationToken.None);
        session.SetBreakpoint(breakpointLine);

        Task<PausedEventArgs> pausedTask = TestDebugPrograms.PreparePausedWaitAsync(session);
        session.Continue();
        await pausedTask;

        return session;
    }
}
