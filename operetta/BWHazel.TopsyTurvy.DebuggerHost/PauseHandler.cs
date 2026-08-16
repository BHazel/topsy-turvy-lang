using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Debugger;
using OmniSharp.Extensions.DebugAdapter.Protocol.Requests;

namespace BWHazel.TopsyTurvy.DebuggerHost;

/// <summary>
/// Requests an on-demand pause at the next statement boundary.
/// </summary>
/// <param name="session">The session this host manages.</param>
/// <remarks>
/// This handler handles the following DAP request:
/// * <c>pause</c>: The client requests that the target be paused.
/// </remarks>
public sealed class PauseHandler(DebugSession session) : PauseHandlerBase
{
    /// <inheritdoc/>
    public override Task<PauseResponse> Handle(PauseArguments request, CancellationToken cancellationToken)
    {
        session.Pause();
        return Task.FromResult(new PauseResponse());
    }
}
