using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Debugger;
using BWHazel.TopsyTurvy.DebuggerHost;
using BWHazel.TopsyTurvy.Tests.Debugger;
using OmniSharp.Extensions.DebugAdapter.Protocol.Models;
using OmniSharp.Extensions.DebugAdapter.Protocol.Requests;

using DapBreakpoint = OmniSharp.Extensions.DebugAdapter.Protocol.Models.Breakpoint;
using TopsyTurvyBreakpoint = BWHazel.TopsyTurvy.Debugger.Breakpoint;

namespace BWHazel.TopsyTurvy.Tests.DebuggerHost;

/// <summary>
/// Tests for the <see cref="SetBreakpointsHandler"/> class.
/// </summary>
public class SetBreakpointsHandlerTests
{
    /// <summary>
    /// Tests that the <see cref="SetBreakpointsHandler.Handle"/> method returns a verified breakpoint for a line
    /// with a statement.
    /// </summary>
    [Fact]
    public async Task Handle_WithLineHavingStatement_ReturnsVerifiedBreakpoint()
    {
        DebugSession session = new(TestDebugPrograms.Resolver);
        await session.StartAsync(TestDebugPrograms.SimpleFileName, CancellationToken.None);
        SetBreakpointsHandler handler = new(session);

        SetBreakpointsResponse response = await handler.Handle(RequestFor(TestDebugPrograms.SimpleAssignmentLine), CancellationToken.None);

        DapBreakpoint breakpoint = response.Breakpoints.ShouldHaveSingleItem();
        breakpoint.Verified.ShouldBeTrue();
        breakpoint.Line.ShouldBe(TestDebugPrograms.SimpleAssignmentLine);
    }

    /// <summary>
    /// Tests that the <see cref="SetBreakpointsHandler.Handle"/> method replaces the previous breakpoint set
    /// rather than adding to it, since a DAP client always sends the complete desired set.
    /// </summary>
    [Fact]
    public async Task Handle_CalledTwice_ReplacesPreviousBreakpointSet()
    {
        DebugSession session = new(TestDebugPrograms.Resolver);
        await session.StartAsync(TestDebugPrograms.SimpleFileName, CancellationToken.None);
        SetBreakpointsHandler handler = new(session);

        await handler.Handle(RequestFor(TestDebugPrograms.SimpleAssignmentLine), CancellationToken.None);
        await handler.Handle(RequestFor(3), CancellationToken.None);

        TopsyTurvyBreakpoint breakpoint = session.GetBreakpoints().ShouldHaveSingleItem();
        breakpoint.Line.ShouldBe(3);
    }

    /// <summary>
    /// Creates a <see cref="SetBreakpointsArguments"/> request for the given line number.
    /// </summary>
    /// <param name="line">The line number for the breakpoint.</param>
    /// <returns>A <see cref="SetBreakpointsArguments"/> instance for the given line.</returns>
    private static SetBreakpointsArguments RequestFor(int line) => new()
    {
        Source = new Source { Path = TestDebugPrograms.SimpleFileName },
        Breakpoints = new Container<SourceBreakpoint>(new SourceBreakpoint()
        {
            Line = line
        })
    };
}
