using System;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Debugger;
using OmniSharp.Extensions.DebugAdapter.Protocol.Requests;

namespace BWHazel.TopsyTurvy.DebuggerHost;

/// <summary>
/// Stops a session and signals the host process to exit once the client disconnects.
/// </summary>
/// <param name="session">The session this host manages.</param>
/// <param name="exitSignal">The signal completed once the request is handled, so the host process exits.</param>
/// <remarks>
/// <para>
/// This handler handles the following DAP request:
/// * <c>disconnect</c>: The client is ending the session; the debuggee, if still running, should stop.
/// </para>
/// <para>
/// <see cref="DebugAdapterExitSignal.Complete"/> is deferred to a background delay rather than called inline,
/// since <see cref="DebugAdapterHost.RunAsync"/> disposes the underlying connection as soon as it observes the
/// signal, which would otherwise race the response for this very request still being flushed to the output pipe.
/// </para>
/// </remarks>
public sealed class DisconnectHandler(DebugSession session, DebugAdapterExitSignal exitSignal) : DisconnectHandlerBase
{
    /// <inheritdoc/>
    public override Task<DisconnectResponse> Handle(DisconnectArguments request, CancellationToken cancellationToken)
    {
        session.Stop();
        _ = Task.Delay(TimeSpan.FromMilliseconds(200)).ContinueWith(
            _ => exitSignal.Complete(),
            TaskScheduler.Default);

        return Task.FromResult(new DisconnectResponse());
    }
}
