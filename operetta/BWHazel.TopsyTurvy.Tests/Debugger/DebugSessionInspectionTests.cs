using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Debugger;

namespace BWHazel.TopsyTurvy.Tests.Debugger;

/// <summary>
/// Call-stack and variable-scope inspection tests for the <see cref="DebugSession"/> class.
/// </summary>
public class DebugSessionInspectionTests
{
    /// <summary>
    /// Tests that the <see cref="DebugSession.GetCallStack"/> method lists the top-level frame and an active
    /// function call, most-recent call first.
    /// </summary>
    [Fact]
    public async Task GetCallStack_WhilePausedInsideFunction_ListsCalleeThenTopLevelFrame()
    {
        DebugSession session = new(TestDebugPrograms.Resolver);
        await session.StartAsync(TestDebugPrograms.FixtureFileName, CancellationToken.None);
        session.SetBreakpoint(TestDebugPrograms.FunctionLocalDeclarationLine);

        Task<PausedEventArgs> pausedTask = TestDebugPrograms.PreparePausedWaitAsync(session);
        session.Continue();
        await pausedTask;

        IReadOnlyList<StackFrame> frames = session.GetCallStack();

        frames.Count.ShouldBe(2);
        frames[0].Name.ShouldBe("AddOne");
        frames[1].Name.ShouldBe("<programme>");
    }

    /// <summary>
    /// Tests that the <see cref="DebugSession.GetVariables"/> method reports the parameter of a function frame, and
    /// no <c>"Globals"</c> scope.
    /// </summary>
    [Fact]
    public async Task GetVariables_ForFunctionFrame_ReportsLocalsOnlyWithNoGlobalsScope()
    {
        DebugSession session = new(TestDebugPrograms.Resolver);
        await session.StartAsync(TestDebugPrograms.FixtureFileName, CancellationToken.None);
        session.SetBreakpoint(TestDebugPrograms.FunctionLocalDeclarationLine);

        Task<PausedEventArgs> pausedTask = TestDebugPrograms.PreparePausedWaitAsync(session);
        session.Continue();
        await pausedTask;

        int functionFrameId = session.GetCallStack()[0].Id;
        IReadOnlyList<VariableScope> scopes = session.GetVariables(functionFrameId);

        scopes.Count.ShouldBe(1);
        scopes[0].Label.ShouldBe("Locals");
        scopes[0].Variables["n"].RawValue.ShouldBe(5);
        scopes.ShouldNotContain(scope => scope.Label == "Globals");
    }

    /// <summary>
    /// Tests that the <see cref="DebugSession.GetVariables"/> method reports a <c>"Globals"</c> scope containing
    /// both a <c>PRINCIPALS</c> declaration and a later top-level declaration when inspecting the top-level frame.
    /// </summary>
    [Fact]
    public async Task GetVariables_ForTopLevelFrame_ReportsBothDeclarationsInGlobalsScope()
    {
        DebugSession session = new(TestDebugPrograms.Resolver);
        await session.StartAsync(TestDebugPrograms.FixtureFileName, CancellationToken.None);
        session.SetBreakpoint(TestDebugPrograms.FunctionLocalDeclarationLine);

        Task<PausedEventArgs> pausedTask = TestDebugPrograms.PreparePausedWaitAsync(session);
        session.Continue();
        await pausedTask;

        int topLevelFrameId = session.GetCallStack()[1].Id;
        IReadOnlyList<VariableScope> scopes = session.GetVariables(topLevelFrameId);

        VariableScope globals = scopes.Single(scope => scope.Label == "Globals");
        globals.Variables["globalValue"].RawValue.ShouldBe(100);
        globals.Variables["result"].RawValue.ShouldBe(0);
    }

    /// <summary>
    /// Tests that the <see cref="DebugSession.GetVariables"/> method throws when given a frame ID that is not in
    /// the current call stack.
    /// </summary>
    [Fact]
    public async Task GetVariables_WithUnknownFrameId_ThrowsArgumentException()
    {
        DebugSession session = new(TestDebugPrograms.Resolver);
        await session.StartAsync(TestDebugPrograms.SimpleFileName, CancellationToken.None);
        session.SetBreakpoint(TestDebugPrograms.SimpleAssignmentLine);

        Task<PausedEventArgs> pausedTask = TestDebugPrograms.PreparePausedWaitAsync(session);
        session.Continue();
        await pausedTask;

        Should.Throw<ArgumentException>(() => session.GetVariables(-1));
    }
}
