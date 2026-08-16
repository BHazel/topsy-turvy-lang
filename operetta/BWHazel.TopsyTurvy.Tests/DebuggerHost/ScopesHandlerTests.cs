using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Debugger;
using BWHazel.TopsyTurvy.DebuggerHost;
using BWHazel.TopsyTurvy.Tests.Debugger;
using OmniSharp.Extensions.DebugAdapter.Protocol.Models;
using OmniSharp.Extensions.DebugAdapter.Protocol.Requests;

namespace BWHazel.TopsyTurvy.Tests.DebuggerHost;

/// <summary>
/// Tests for the <see cref="ScopesHandler"/> class.
/// </summary>
public class ScopesHandlerTests : DebuggerHostTestBase
{
    /// <summary>
    /// Tests that the <see cref="ScopesHandler.Handle"/> method returns only a <c>Locals</c> scope for a function frame.
    /// </summary>
    [Fact]
    public async Task Handle_ForFunctionFrame_ReturnsLocalsScopeOnly()
    {
        DebugSession session = await CreatePausedSessionAsync(TestDebugPrograms.FunctionLocalDeclarationLine);
        int functionFrameId = session.GetCallStack()[0].Id;
        ScopesHandler handler = new(session);

        ScopesResponse response = await handler.Handle(new ScopesArguments()
        {
            FrameId = functionFrameId
        },
        CancellationToken.None);

        Scope scope = response.Scopes.ShouldHaveSingleItem();
        scope.Name.ShouldBe("Locals");
    }

    /// <summary>
    /// Tests that the <see cref="ScopesHandler.Handle"/> method returns both a <c>Locals</c> and a <c>Globals</c>
    /// scope for the top-level frame.
    /// </summary>
    [Fact]
    public async Task Handle_ForTopLevelFrame_ReturnsLocalsAndGlobalsScopes()
    {
        DebugSession session = await CreatePausedSessionAsync(TestDebugPrograms.FunctionLocalDeclarationLine);
        int topLevelFrameId = session.GetCallStack()[1].Id;
        ScopesHandler handler = new(session);

        ScopesResponse response = await handler.Handle(new ScopesArguments()
        {
            FrameId = topLevelFrameId
        },
        CancellationToken.None);

        List<Scope> scopes = [.. response.Scopes];
        scopes.Select(scope => scope.Name).ShouldBe(["Locals", "Globals"]);
    }
}
