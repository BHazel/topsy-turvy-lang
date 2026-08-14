using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Debugger;

namespace BWHazel.TopsyTurvy.Tests.Debugger;

/// <summary>
/// Expression evaluation tests for the <see cref="DebugSession"/> class.
/// </summary>
public class DebugSessionEvaluationTests
{
    /// <summary>
    /// Tests that the <see cref="DebugSession.Evaluate"/> method evaluates an expression combining an in-scope
    /// variable with a literal.
    /// </summary>
    [Fact]
    public async Task Evaluate_WithExpressionUsingInScopeVariable_ReturnsCorrectValue()
    {
        DebugSession session = new(TestDebugPrograms.Resolver);
        await session.StartAsync(TestDebugPrograms.FixtureFileName, CancellationToken.None);
        session.SetBreakpoint(TestDebugPrograms.FunctionLocalDeclarationLine);

        Task<PausedEventArgs> pausedTask = TestDebugPrograms.PreparePausedWaitAsync(session);
        session.Continue();
        await pausedTask;

        int functionFrameId = session.GetCallStack()[0].Id;
        EvaluationResult result = session.Evaluate("SUM OF n AND 10", functionFrameId);

        result.IsSuccess.ShouldBeTrue();
        result.Value!.RawValue.ShouldBe(15);
    }

    /// <summary>
    /// Tests that the <see cref="DebugSession.Evaluate"/> method reports a parse failure for an invalid expression
    /// without throwing.
    /// </summary>
    [Fact]
    public async Task Evaluate_WithInvalidExpression_ReturnsFailureResult()
    {
        DebugSession session = new(TestDebugPrograms.Resolver);
        await session.StartAsync(TestDebugPrograms.SimpleFileName, CancellationToken.None);
        session.SetBreakpoint(TestDebugPrograms.SimpleAssignmentLine);

        Task<PausedEventArgs> pausedTask = TestDebugPrograms.PreparePausedWaitAsync(session);
        session.Continue();
        await pausedTask;

        int frameId = session.GetCallStack()[0].Id;
        EvaluationResult result = session.Evaluate("SUM OF", frameId);

        result.IsSuccess.ShouldBeFalse();
        result.ErrorMessage.ShouldNotBeNull();
    }

    /// <summary>
    /// Tests that the <see cref="DebugSession.Evaluate"/> method reports a failure for a reference to an undeclared
    /// variable, without throwing and without changing the value of any existing variable.
    /// </summary>
    [Fact]
    public async Task Evaluate_WithUndeclaredVariable_ReturnsFailureResultAndLeavesVariablesUnchanged()
    {
        DebugSession session = new(TestDebugPrograms.Resolver);
        await session.StartAsync(TestDebugPrograms.SimpleFileName, CancellationToken.None);
        session.SetBreakpoint(TestDebugPrograms.SimpleAssignmentLine);

        Task<PausedEventArgs> pausedTask = TestDebugPrograms.PreparePausedWaitAsync(session);
        session.Continue();
        await pausedTask;

        int frameId = session.GetCallStack()[0].Id;
        EvaluationResult before = session.Evaluate("result", frameId);

        EvaluationResult result = session.Evaluate("doesNotExist", frameId);

        result.IsSuccess.ShouldBeFalse();
        EvaluationResult after = session.Evaluate("result", frameId);
        after.Value!.RawValue.ShouldBe(before.Value!.RawValue);
    }
}
