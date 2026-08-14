using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Debugger;

namespace BWHazel.TopsyTurvy.Tests.Debugger;

/// <summary>
/// Breakpoint set, remove and verification tests for the <see cref="DebugSession"/> class.
/// </summary>
public class DebugSessionBreakpointTests
{
    /// <summary>
    /// Tests that the <see cref="DebugSession.SetBreakpoint"/> method marks a breakpoint on a line with a
    /// statement as verified.
    /// </summary>
    [Fact]
    public async Task SetBreakpoint_OnLineWithStatement_ReturnsVerifiedBreakpoint()
    {
        DebugSession session = new(TestDebugPrograms.Resolver);
        await session.StartAsync(TestDebugPrograms.SimpleFileName, CancellationToken.None);

        Breakpoint breakpoint = session.SetBreakpoint(TestDebugPrograms.SimpleAssignmentLine);

        breakpoint.IsVerified.ShouldBeTrue();
        breakpoint.Line.ShouldBe(TestDebugPrograms.SimpleAssignmentLine);
    }

    /// <summary>
    /// Tests that the <see cref="DebugSession.SetBreakpoint"/> method marks a breakpoint on a blank line as
    /// unverified.
    /// </summary>
    [Fact]
    public async Task SetBreakpoint_OnBlankLine_ReturnsUnverifiedBreakpoint()
    {
        DebugSession session = new(TestDebugPrograms.Resolver);
        await session.StartAsync(TestDebugPrograms.SimpleFileName, CancellationToken.None);

        Breakpoint breakpoint = session.SetBreakpoint(3);

        breakpoint.IsVerified.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that the <see cref="DebugSession.RemoveBreakpoint"/> method removes a previously set breakpoint so it
    /// is no longer hit.
    /// </summary>
    [Fact]
    public async Task RemoveBreakpoint_AfterSet_ExecutionRunsToCompletionWithoutPausing()
    {
        DebugSession session = new(TestDebugPrograms.Resolver);
        await session.StartAsync(TestDebugPrograms.SimpleFileName, CancellationToken.None);
        Breakpoint breakpoint = session.SetBreakpoint(TestDebugPrograms.SimpleAssignmentLine);
        
        session.RemoveBreakpoint(breakpoint.Id);

        Task<EndedEventArgs> endedTask = TestDebugPrograms.PrepareEndedWaitAsync(session);
        session.Continue();
        await endedTask;

        session.Status.ShouldBe(SessionStatus.Ended);
    }

    /// <summary>
    /// Tests that the <see cref="DebugSession.SetBreakpoint"/> method is valid before the session has ever resumed.
    /// </summary>
    [Fact]
    public async Task SetBreakpoint_BeforeFirstResume_Succeeds()
    {
        DebugSession session = new(TestDebugPrograms.Resolver);
        await session.StartAsync(TestDebugPrograms.SimpleFileName, CancellationToken.None);
        session.Status.ShouldBe(SessionStatus.Paused);
        
        Breakpoint breakpoint = session.SetBreakpoint(TestDebugPrograms.SimpleAssignmentLine);
        
        breakpoint.IsVerified.ShouldBeTrue();
    }
}
