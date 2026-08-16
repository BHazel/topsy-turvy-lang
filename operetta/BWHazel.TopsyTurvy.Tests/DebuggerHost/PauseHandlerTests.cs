using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Debugger;
using BWHazel.TopsyTurvy.DebuggerHost;
using BWHazel.TopsyTurvy.Tests.Debugger;
using OmniSharp.Extensions.DebugAdapter.Protocol.Requests;

namespace BWHazel.TopsyTurvy.Tests.DebuggerHost;

/// <summary>
/// Tests for the <see cref="PauseHandler"/> class.
/// </summary>
public class PauseHandlerTests : DebuggerHostTestBase
{
    private const string InfiniteLoopSource = """
        HARK! "Infinite"
        PRAY WELCOME counter AS A PEER BEING 0
        BY A LEGAL FICTION
          counter IS APPOINTED SUM OF counter AND 1
        THE TERM EXPIRES.
        FINALE.
        """;

    /// <summary>
    /// Tests that the <see cref="PauseHandler.Handle"/> method pauses a running session on demand.
    /// </summary>
    [Fact]
    public async Task PauseHandler_Handle_PausesRunningSessionOnDemand()
    {
        DebugSession session = new(name => name == "infinite.topsy" ? InfiniteLoopSource : null);
        await session.StartAsync("infinite.topsy", CancellationToken.None);
        PauseHandler handler = new(session);

        session.Continue();
        await Task.Delay(50);

        Task<PausedEventArgs> pausedTask = TestDebugPrograms.PreparePausedWaitAsync(session);
        await handler.Handle(new PauseArguments(), CancellationToken.None);
        PausedEventArgs paused = await pausedTask;

        paused.Reason.ShouldBe(PauseReason.ManualPause);
        session.Stop();
    }
}
