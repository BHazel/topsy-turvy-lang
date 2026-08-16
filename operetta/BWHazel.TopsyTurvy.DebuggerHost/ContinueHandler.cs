using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Debugger;
using OmniSharp.Extensions.DebugAdapter.Protocol.Requests;

namespace BWHazel.TopsyTurvy.DebuggerHost;

/// <summary>
/// Resumes a paused session, running until the next breakpoint, unhandled error, or completion.
/// </summary>
/// <param name="session">The session this host manages.</param>
/// <remarks>
/// This handler handles the following DAP request:
/// * <c>continue</c>: The client requests that execution continue.
/// </remarks>
public sealed class ContinueHandler(DebugSession session) : ContinueHandlerBase
{
    /// <inheritdoc/>
    public override Task<ContinueResponse> Handle(ContinueArguments request, CancellationToken cancellationToken)
    {
        session.Continue();
        return Task.FromResult(new ContinueResponse()
        {
            AllThreadsContinued = true
        });
    }
}
