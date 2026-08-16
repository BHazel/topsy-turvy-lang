using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Debugger;
using BWHazel.TopsyTurvy.DebuggerHost;
using BWHazel.TopsyTurvy.Tests.Debugger;
using OmniSharp.Extensions.DebugAdapter.Protocol.Requests;

namespace BWHazel.TopsyTurvy.Tests.DebuggerHost;

/// <summary>
/// Tests for the <see cref="StepInHandler"/> class.
/// </summary>
public class StepInHandlerTests : DebuggerHostTestBase
{
    /// <summary>
    /// Tests that the <see cref="StepInHandler.Handle"/> method descends into the body of a called function.
    /// </summary>
    [Fact]
    public async Task StepInHandler_Handle_DescendsIntoCalledFunction()
    {
        DebugSession session = await CreatePausedSessionAsync(11);
        StepInHandler handler = new(session);

        Task<PausedEventArgs> pausedTask = TestDebugPrograms.PreparePausedWaitAsync(session);
        await handler.Handle(new StepInArguments(), CancellationToken.None);
        PausedEventArgs paused = await pausedTask;

        paused.Reason.ShouldBe(PauseReason.StepComplete);
        session.GetCallStack().Count.ShouldBe(2);
    }
}
