using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Debugger;
using OmniSharp.Extensions.DebugAdapter.Protocol.Models;
using OmniSharp.Extensions.DebugAdapter.Protocol.Requests;

using DapStackFrame = OmniSharp.Extensions.DebugAdapter.Protocol.Models.StackFrame;

namespace BWHazel.TopsyTurvy.DebuggerHost;

/// <summary>
/// Returns the current call stack of a paused session.
/// </summary>
/// <param name="session">The session this host manages.</param>
/// <remarks>
/// <para>
/// This handler handles the following DAP request:
/// * <c>stackTrace</c>: The client requests a stack trace from the current execution state.
/// </para>
/// <para>
/// Every frame gets the same <see cref="Source"/>, since a paused call stack only ever contains frames from the
/// one file a session debugs: an opaque frame for a <c>PRAY ADMIT</c> import never pauses. Without it, a DAP
/// client has no file to highlight a paused line in or navigate to from a call stack view.
/// </para>
/// </remarks>
public sealed class StackTraceHandler(DebugSession session) : StackTraceHandlerBase
{
    /// <inheritdoc/>
    public override Task<StackTraceResponse> Handle(StackTraceArguments request, CancellationToken cancellationToken)
    {
        IReadOnlyList<Debugger.StackFrame> frames = session.GetCallStack();
        Source source = new()
        {
            Path = session.SourceFilePath
        };

        List<DapStackFrame> dapFrames = [.. frames.Select(frame => new DapStackFrame()
        {
            Id = frame.Id,
            Name = frame.Name,
            Source = source,
            Line = frame.Span.Start.Line,
            Column = frame.Span.Start.Column
        })];

        return Task.FromResult(new StackTraceResponse()
        {
            StackFrames = new Container<DapStackFrame>(dapFrames),
            TotalFrames = dapFrames.Count
        });
    }
}
