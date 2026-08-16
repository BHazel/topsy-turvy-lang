using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Debugger;
using OmniSharp.Extensions.DebugAdapter.Protocol.Requests;

namespace BWHazel.TopsyTurvy.DebuggerHost;

/// <summary>
/// Resumes execution until the current frame returns to its caller.
/// </summary>
/// <param name="session">The session this host manages.</param>
/// <remarks>
/// This handler handles the following DAP request:
/// * <c>stepOut</c>: The client requests that execution continue and step out of the current function.
/// </remarks>
public sealed class StepOutHandler(DebugSession session) : StepOutHandlerBase
{
    /// <inheritdoc/>
    public override Task<StepOutResponse> Handle(StepOutArguments request, CancellationToken cancellationToken)
    {
        session.StepOut();
        return Task.FromResult(new StepOutResponse());
    }
}
