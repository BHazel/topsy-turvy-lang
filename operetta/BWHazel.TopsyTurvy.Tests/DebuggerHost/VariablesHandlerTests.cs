using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Debugger;
using BWHazel.TopsyTurvy.DebuggerHost;
using BWHazel.TopsyTurvy.Tests.Debugger;
using OmniSharp.Extensions.DebugAdapter.Protocol.Models;
using OmniSharp.Extensions.DebugAdapter.Protocol.Requests;

namespace BWHazel.TopsyTurvy.Tests.DebuggerHost;

/// <summary>
/// Tests for the <see cref="VariablesHandler"/> class.
/// </summary>
public class VariablesHandlerTests : DebuggerHostTestBase
{
    /// <summary>
    /// Tests that the <see cref="VariablesHandler.Handle"/> method reports the correct name and value for a
    /// function parameter, given the variables reference of the function's <c>Locals</c> scope.
    /// </summary>
    [Fact]
    public async Task Handle_WithLocalsScopeReference_ReportsFunctionParameter()
    {
        DebugSession session = await CreatePausedSessionAsync(TestDebugPrograms.FunctionLocalDeclarationLine);
        int functionFrameId = session.GetCallStack()[0].Id;
        long localsReference = VariablesReferenceCodec.Encode(functionFrameId, scopeIndex: 0);
        VariablesHandler handler = new(session);

        VariablesResponse response = await handler.Handle(new VariablesArguments()
        {
            VariablesReference = localsReference
        },
        CancellationToken.None);

        Variable variable = response.Variables.ShouldHaveSingleItem();
        variable.Name.ShouldBe("n");
        variable.Value.ShouldBe("5");
    }
}
