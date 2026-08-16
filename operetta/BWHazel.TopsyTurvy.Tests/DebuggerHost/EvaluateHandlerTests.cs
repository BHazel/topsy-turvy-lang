using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Debugger;
using BWHazel.TopsyTurvy.DebuggerHost;
using BWHazel.TopsyTurvy.Tests.Debugger;
using OmniSharp.Extensions.DebugAdapter.Protocol.Requests;

namespace BWHazel.TopsyTurvy.Tests.DebuggerHost;

/// <summary>
/// Tests for the <see cref="EvaluateHandler"/> class.
/// </summary>
public class EvaluateHandlerTests : DebuggerHostTestBase
{
    /// <summary>
    /// Tests that the <see cref="EvaluateHandler.Handle"/> method evaluates an expression against the given frame.
    /// </summary>
    [Fact]
    public async Task Handle_WithValidExpression_ReturnsEvaluatedResult()
    {
        DebugSession session = await CreatePausedSessionAsync(TestDebugPrograms.FunctionLocalDeclarationLine);
        int functionFrameId = session.GetCallStack()[0].Id;
        EvaluateHandler handler = new(session);

        EvaluateResponse response = await handler.Handle(
            new EvaluateArguments()
            {
                Expression = "n",
                FrameId = functionFrameId
            },
            CancellationToken.None);

        response.Result.ShouldBe("5");
    }

    /// <summary>
    /// Tests that the <see cref="EvaluateHandler.Handle"/> method falls back to the top frame when no frame ID is
    /// given.
    /// </summary>
    /// <remarks>
    /// An example is an evaluate request sent from the Debug Console with nothing selected in the Call Stack panel.
    /// </remarks>
    [Fact]
    public async Task Handle_WithNoFrameId_FallsBackToTopFrame()
    {
        DebugSession session = await CreatePausedSessionAsync(TestDebugPrograms.FunctionLocalDeclarationLine);
        EvaluateHandler handler = new(session);

        EvaluateResponse response = await handler.Handle(new EvaluateArguments()
        {
            Expression = "n"
        },
        CancellationToken.None);

        response.Result.ShouldBe("5");
    }

    /// <summary>
    /// Tests that the <see cref="EvaluateHandler.Handle"/> method reports a failed evaluation as an <c>Error:</c>
    /// prefixed result rather than a thrown exception, since a mistyped watch expression is a routine outcome.
    /// </summary>
    [Fact]
    public async Task Handle_WithUnknownVariable_ReturnsErrorResult()
    {
        DebugSession session = await CreatePausedSessionAsync(TestDebugPrograms.FunctionLocalDeclarationLine);
        int functionFrameId = session.GetCallStack()[0].Id;
        EvaluateHandler handler = new(session);

        EvaluateResponse response = await handler.Handle(
            new EvaluateArguments()
            {
                Expression = "doesNotExist",
                FrameId = functionFrameId
            },
            CancellationToken.None);

        response.Result.ShouldStartWith("Error:");
    }
}
