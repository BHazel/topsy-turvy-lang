using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Debugger;
using BWHazel.TopsyTurvy.DebuggerHost;
using BWHazel.TopsyTurvy.Tests.Debugger;
using OmniSharp.Extensions.DebugAdapter.Protocol.Requests;

namespace BWHazel.TopsyTurvy.Tests.DebuggerHost;

/// <summary>
/// Tests for the <see cref="StepOutHandler"/> class.
/// </summary>
public class StepOutHandlerTests : DebuggerHostTestBase
{
    /// <summary>
    /// Tests that the <see cref="StepOutHandler.Handle"/> method resumes until the current frame returns to its caller.
    /// </summary>
    [Fact]
    public async Task StepOutHandler_Handle_ResumesUntilCallerFrame()
    {
        DebugSession session = await CreatePausedSessionAsync(TestDebugPrograms.FunctionLocalDeclarationLine);
        session.GetCallStack().Count.ShouldBe(2);
        StepOutHandler handler = new(session);

        Task<PausedEventArgs> pausedTask = TestDebugPrograms.PreparePausedWaitAsync(session);
        await handler.Handle(new StepOutArguments(), CancellationToken.None);
        PausedEventArgs paused = await pausedTask;

        paused.Reason.ShouldBe(PauseReason.StepComplete);
        session.GetCallStack().Count.ShouldBe(1);
    }
}
