using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Debugger;
using OmniSharp.Extensions.DebugAdapter.Protocol.Models;
using OmniSharp.Extensions.DebugAdapter.Protocol.Requests;

using DapBreakpoint = OmniSharp.Extensions.DebugAdapter.Protocol.Models.Breakpoint;
using TopsyTurvyBreakpoint = BWHazel.TopsyTurvy.Debugger.Breakpoint;

namespace BWHazel.TopsyTurvy.DebuggerHost;

/// <summary>
/// Replaces the active breakpoints for the debugged file with the given set.
/// </summary>
/// <param name="session">The session this host manages.</param>
/// <remarks>
/// <para>
/// This handler handles the following DAP request:
/// * <c>setBreakpoints</c>: Sets multiple breakpoints for a single source and clears all previous breakpoints in
/// that source.
/// </para>
/// <para>
/// A DAP client always sends the complete desired breakpoint set for a source file on every call, so every
/// previously active breakpoint is removed before the requested set is applied fresh.
/// </para>
/// </remarks>
public sealed class SetBreakpointsHandler(DebugSession session) : SetBreakpointsHandlerBase
{
    /// <inheritdoc/>
    public override Task<SetBreakpointsResponse> Handle(SetBreakpointsArguments request, CancellationToken cancellationToken)
    {
        foreach (TopsyTurvyBreakpoint existingBreakpoint in session.GetBreakpoints())
        {
            session.RemoveBreakpoint(existingBreakpoint.Id);
        }

        List<DapBreakpoint> verifiedBreakpoints = [];
        foreach (SourceBreakpoint requestedBreakpoint in (IEnumerable<SourceBreakpoint>?)request.Breakpoints ?? Enumerable.Empty<SourceBreakpoint>())
        {
            TopsyTurvyBreakpoint breakpoint = session.SetBreakpoint(requestedBreakpoint.Line);
            verifiedBreakpoints.Add(new DapBreakpoint()
            {
                Id = breakpoint.Id,
                Verified = breakpoint.IsVerified,
                Line = breakpoint.Line
            });
        }

        return Task.FromResult(new SetBreakpointsResponse()
        {
            Breakpoints = new Container<DapBreakpoint>(verifiedBreakpoints)
        });
    }
}
