using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Debugger;
using BWHazel.TopsyTurvy.DebuggerHost;
using BWHazel.TopsyTurvy.Tests.Debugger;
using OmniSharp.Extensions.DebugAdapter.Protocol.Requests;

namespace BWHazel.TopsyTurvy.Tests.DebuggerHost;

/// <summary>
/// Tests for the <see cref="ConfigurationDoneHandler"/> class.
/// </summary>
public class ConfigurationDoneHandlerTests
{
    /// <summary>
    /// Tests that the <see cref="ConfigurationDoneHandler.Handle"/> method starts the target programme running for
    /// the first time.
    /// </summary>
    [Fact]
    public async Task Handle_Always_StartsProgrammeRunning()
    {
        DebugSession session = new(TestDebugPrograms.Resolver);
        await session.StartAsync(TestDebugPrograms.SimpleFileName, CancellationToken.None);
        ConfigurationDoneHandler handler = new(session);

        Task<EndedEventArgs> endedTask = TestDebugPrograms.PrepareEndedWaitAsync(session);
        await handler.Handle(new ConfigurationDoneArguments(), CancellationToken.None);
        await endedTask;

        session.Status.ShouldBe(SessionStatus.Ended);
    }
}
