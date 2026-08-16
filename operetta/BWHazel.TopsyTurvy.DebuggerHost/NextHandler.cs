using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Debugger;
using OmniSharp.Extensions.DebugAdapter.Protocol.Requests;

namespace BWHazel.TopsyTurvy.DebuggerHost;

/// <summary>
/// Resumes execution until the next statement in the current frame, without descending into a called function.
/// </summary>
/// <param name="session">The session this host manages.</param>
/// <remarks>
/// This handler handles the following DAP request:
/// * <c>next</c>: The client requests that execution continue to the next source line without stepping into a
/// called function (a "step over").
/// </remarks>
public sealed class NextHandler(DebugSession session) : NextHandlerBase
{
    /// <inheritdoc/>
    public override Task<NextResponse> Handle(NextArguments request, CancellationToken cancellationToken)
    {
        session.StepOver();
        return Task.FromResult(new NextResponse());
    }
}
