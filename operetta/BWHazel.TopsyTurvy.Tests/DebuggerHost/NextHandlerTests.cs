using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Debugger;
using BWHazel.TopsyTurvy.DebuggerHost;
using BWHazel.TopsyTurvy.Tests.Debugger;
using OmniSharp.Extensions.DebugAdapter.Protocol.Requests;

namespace BWHazel.TopsyTurvy.Tests.DebuggerHost;

/// <summary>
/// Tests for the <see cref="NextHandler"/> class.
/// </summary>
public class NextHandlerTests : DebuggerHostTestBase
{
    /// <summary>
    /// Tests that the <see cref="NextHandler.Handle"/> method steps over the current statement without descending
    /// into a call.
    /// </summary>
    [Fact]
    public async Task NextHandler_Handle_StepsOverCurrentStatement()
    {
        DebugSession session = await CreatePausedSessionAsync(11);
        NextHandler handler = new(session);

        Task<PausedEventArgs> pausedTask = TestDebugPrograms.PreparePausedWaitAsync(session);
        await handler.Handle(new NextArguments(), CancellationToken.None);
        PausedEventArgs paused = await pausedTask;

        paused.Reason.ShouldBe(PauseReason.StepComplete);
        session.GetCallStack().Count.ShouldBe(1);
    }
}
