using System;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Debugger;
using BWHazel.TopsyTurvy.DebuggerHost;
using BWHazel.TopsyTurvy.Tests.Debugger;
using OmniSharp.Extensions.DebugAdapter.Protocol.Requests;

namespace BWHazel.TopsyTurvy.Tests.DebuggerHost;

/// <summary>
/// Tests for the <see cref="DisconnectHandler"/> class.
/// </summary>
public class DisconnectHandlerTests
{
    /// <summary>
    /// Tests that the <see cref="DisconnectHandler.Handle"/> method stops the session and eventually completes the
    /// shared <see cref="DebugAdapterExitSignal"/>.
    /// </summary>
    [Fact]
    public async Task Handle_Always_StopsSessionAndCompletesExitSignal()
    {
        DebugSession session = new(TestDebugPrograms.Resolver);
        await session.StartAsync(TestDebugPrograms.SimpleFileName, CancellationToken.None);
        DebugAdapterExitSignal exitSignal = new();
        DisconnectHandler handler = new(session, exitSignal);

        await handler.Handle(new DisconnectArguments(), CancellationToken.None);

        session.Status.ShouldBe(SessionStatus.Ended);
        await exitSignal.Exited.WaitAsync(TimeSpan.FromSeconds(2));
    }
}
