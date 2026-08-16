using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Debugger;
using BWHazel.TopsyTurvy.DebuggerHost;
using BWHazel.TopsyTurvy.Tests.Debugger;
using OmniSharp.Extensions.DebugAdapter.Protocol.Requests;
using DapStackFrame = OmniSharp.Extensions.DebugAdapter.Protocol.Models.StackFrame;

namespace BWHazel.TopsyTurvy.Tests.DebuggerHost;

/// <summary>
/// Tests for the <see cref="StackTraceHandler"/> class.
/// </summary>
public class StackTraceHandlerTests : DebuggerHostTestBase
{
    /// <summary>
    /// Tests that the <see cref="StackTraceHandler.Handle"/> method lists the callee frame before the top-level
    /// frame, each pointing at the session's source file.
    /// </summary>
    [Fact]
    public async Task Handle_WhilePausedInsideFunction_ListsCalleeThenTopLevelFrame()
    {
        DebugSession session = await CreatePausedSessionAsync(TestDebugPrograms.FunctionLocalDeclarationLine);
        StackTraceHandler handler = new(session);

        StackTraceResponse response = await handler.Handle(new StackTraceArguments(), CancellationToken.None);

        List<DapStackFrame> frames = [.. response.StackFrames.ShouldNotBeNull()];
        response.TotalFrames.ShouldBe(2);
        frames[0].Name.ShouldBe("AddOne");
        frames[0].Source.ShouldNotBeNull().Path.ShouldBe(session.SourceFilePath);
        frames[1].Name.ShouldBe("<programme>");
    }
}
