using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Debugger;
using BWHazel.TopsyTurvy.DebuggerHost;
using BWHazel.TopsyTurvy.Tests.Debugger;
using OmniSharp.Extensions.DebugAdapter.Protocol.Requests;

namespace BWHazel.TopsyTurvy.Tests.DebuggerHost;

/// <summary>
/// Tests for the <see cref="ContinueHandler"/> class.
/// </summary>
public class ContinueHandlerTests : DebuggerHostTestBase
{
    /// <summary>
    /// Tests that the <see cref="ContinueHandler.Handle"/> method resumes a paused session.
    /// </summary>
    [Fact]
    public async Task ContinueHandler_Handle_ResumesPausedSession()
    {
        DebugSession session = await CreatePausedSessionAsync(TestDebugPrograms.FunctionLocalDeclarationLine);
        ContinueHandler handler = new(session);

        Task<EndedEventArgs> endedTask = TestDebugPrograms.PrepareEndedWaitAsync(session);
        await handler.Handle(new ContinueArguments(), CancellationToken.None);
        await endedTask;

        session.Status.ShouldBe(SessionStatus.Ended);
    }
}
