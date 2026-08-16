using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Debugger;
using OmniSharp.Extensions.DebugAdapter.Protocol.Requests;

namespace BWHazel.TopsyTurvy.DebuggerHost;

/// <summary>
/// Resumes execution one statement, descending into a called function if the next statement is a call.
/// </summary>
/// <param name="session">The session this host manages.</param>
/// <remarks>
/// This handler handles the following DAP request:
/// * <c>stepIn</c>: The client requests that execution continue, stepping into a called function.
/// </remarks>
public sealed class StepInHandler(DebugSession session) : StepInHandlerBase
{
    /// <inheritdoc/>
    public override Task<StepInResponse> Handle(StepInArguments request, CancellationToken cancellationToken)
    {
        session.StepIn();
        return Task.FromResult(new StepInResponse());
    }
}
