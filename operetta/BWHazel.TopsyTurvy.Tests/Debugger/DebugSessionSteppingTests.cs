using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Debugger;

namespace BWHazel.TopsyTurvy.Tests.Debugger;

/// <summary>
/// Continue, step-over, step-in, step-out and pause tests for the <see cref="DebugSession"/> class.
/// </summary>
public class DebugSessionSteppingTests
{
    /// <summary>
    /// Tests that the <see cref="DebugSession.Continue"/> method pauses execution immediately before a breakpointed
    /// line runs.
    /// </summary>
    [Fact]
    public async Task Continue_WithBreakpointAhead_PausesAtBreakpointLine()
    {
        DebugSession session = new(TestDebugPrograms.Resolver);
        await session.StartAsync(TestDebugPrograms.SimpleFileName, CancellationToken.None);
        session.SetBreakpoint(TestDebugPrograms.SimpleAssignmentLine);

        Task<PausedEventArgs> pausedTask = TestDebugPrograms.PreparePausedWaitAsync(session);
        session.Continue();
        PausedEventArgs paused = await pausedTask;

        paused.Reason.ShouldBe(PauseReason.BreakpointHit);
        session.Status.ShouldBe(SessionStatus.Paused);
        session.GetCallStack()[0].Span.Start.Line.ShouldBe(TestDebugPrograms.SimpleAssignmentLine);
    }

    /// <summary>
    /// Tests that the <see cref="DebugSession.StepOver"/> method advances past a call to a normal (non-imported)
    /// function without pausing inside it, landing on the next statement in the calling frame.
    /// </summary>
    [Fact]
    public async Task StepOver_OnLineWithFunctionCall_PausesAtNextLineInCallingFrame()
    {
        DebugSession session = new(TestDebugPrograms.Resolver);
        await session.StartAsync(TestDebugPrograms.FixtureFileName, CancellationToken.None);
        session.SetBreakpoint(11);

        Task<PausedEventArgs> breakpointTask = TestDebugPrograms.PreparePausedWaitAsync(session);
        session.Continue();
        await breakpointTask;

        Task<PausedEventArgs> stepTask = TestDebugPrograms.PreparePausedWaitAsync(session);
        session.StepOver();
        PausedEventArgs paused = await stepTask;

        paused.Reason.ShouldBe(PauseReason.StepComplete);
        session.GetCallStack().Count.ShouldBe(1);
        session.GetCallStack()[0].Span.Start.Line.ShouldBe(12); // BEHOLD result in TestDebugPrograms.WithGlobalAndFunctionScope
    }

    /// <summary>
    /// Tests that the <see cref="DebugSession.StepIn"/> method descends into the body of a called function, pausing
    /// at its first statement.
    /// </summary>
    [Fact]
    public async Task StepIn_OnLineWithFunctionCall_PausesAtFirstStatementInCallee()
    {
        DebugSession session = new(TestDebugPrograms.Resolver);
        await session.StartAsync(TestDebugPrograms.FixtureFileName, CancellationToken.None);
        session.SetBreakpoint(11);

        Task<PausedEventArgs> breakpointTask = TestDebugPrograms.PreparePausedWaitAsync(session);
        session.Continue();
        await breakpointTask;

        Task<PausedEventArgs> stepTask = TestDebugPrograms.PreparePausedWaitAsync(session);
        session.StepIn();
        PausedEventArgs paused = await stepTask;

        paused.Reason.ShouldBe(PauseReason.StepComplete);
        session.GetCallStack().Count.ShouldBe(2);
        session.GetCallStack()[0].Name.ShouldBe("AddOne");
        session.GetCallStack()[0].Span.Start.Line.ShouldBe(TestDebugPrograms.FunctionLocalDeclarationLine);
    }

    /// <summary>
    /// Tests that the <see cref="DebugSession.StepOut"/> method resumes until the current frame returns to its
    /// caller, pausing at the statement after the call.
    /// </summary>
    [Fact]
    public async Task StepOut_WhilePausedInsideFunction_PausesAtStatementAfterCallInCaller()
    {
        DebugSession session = new(TestDebugPrograms.Resolver);
        await session.StartAsync(TestDebugPrograms.FixtureFileName, CancellationToken.None);
        session.SetBreakpoint(TestDebugPrograms.FunctionLocalDeclarationLine);

        Task<PausedEventArgs> breakpointTask = TestDebugPrograms.PreparePausedWaitAsync(session);
        session.Continue();
        await breakpointTask;
        session.GetCallStack().Count.ShouldBe(2);

        Task<PausedEventArgs> stepTask = TestDebugPrograms.PreparePausedWaitAsync(session);
        session.StepOut();
        PausedEventArgs paused = await stepTask;

        paused.Reason.ShouldBe(PauseReason.StepComplete);
        session.GetCallStack().Count.ShouldBe(1);
        session.GetCallStack()[0].Span.Start.Line.ShouldBe(12);
    }

    /// <summary>
    /// Tests that the <see cref="DebugSession.StepIn"/> method behaves the same as <see cref="DebugSession.StepOver"/>
    /// when the next call is to a function declared in a file imported via <c>PRAY ADMIT</c>.
    /// </summary>
    [Fact]
    public async Task StepIn_OnCallToImportedFunction_BehavesLikeStepOver()
    {
        const string mainFileName = "main.topsy";
        const string importedFileName = "imported.topsy";
        const int summonLine = 3;
        const int nextLine = 4;

        string importedSource = """
            HARK! "Imported"
            IT IS MY DUTY TO PERFORM Greet UNDER NO OBLIGATION
              BEHOLD "hi"
            MY DUTY IS DISCHARGED.
            FINALE.
            """;

        string mainSource = """
            HARK! "Main"
            PRAY ADMIT "imported.topsy"
            SUMMON Greet WITH NOTHING IF YOU PLEASE.
            BEHOLD "done"
            FINALE.
            """;

        Dictionary<string, string> files = new()
        {
            [mainFileName] = mainSource,
            [importedFileName] = importedSource
        };

        DebugSession session = new(name => files.GetValueOrDefault(name));
        await session.StartAsync(mainFileName, CancellationToken.None);
        session.SetBreakpoint(summonLine);

        Task<PausedEventArgs> breakpointTask = TestDebugPrograms.PreparePausedWaitAsync(session);
        session.Continue();
        await breakpointTask;

        Task<PausedEventArgs> stepTask = TestDebugPrograms.PreparePausedWaitAsync(session);
        session.StepIn();
        PausedEventArgs paused = await stepTask;

        paused.Reason.ShouldBe(PauseReason.StepComplete);
        session.GetCallStack().Count.ShouldBe(1);
        session.GetCallStack()[0].Span.Start.Line.ShouldBe(nextLine);
    }

    /// <summary>
    /// Tests that the <see cref="DebugSession.Pause"/> method pauses a running programme on demand, for example a
    /// non-terminating loop with no breakpoint inside it.
    /// </summary>
    [Fact]
    public async Task Pause_WhileRunningInfiniteLoop_PausesOnDemand()
    {
        const string fileName = "infinite.topsy";
        string source = """
            HARK! "Infinite"
            PRAY WELCOME counter AS A PEER BEING 0
            BY A LEGAL FICTION
              counter IS APPOINTED SUM OF counter AND 1
            THE TERM EXPIRES.
            FINALE.
            """;

        Dictionary<string, string> files = new() { [fileName] = source };
        DebugSession session = new(name => files.GetValueOrDefault(name));
        await session.StartAsync(fileName, CancellationToken.None);

        session.Continue();
        await Task.Delay(50);
        session.Status.ShouldBe(SessionStatus.Running);

        Task<PausedEventArgs> pausedTask = TestDebugPrograms.PreparePausedWaitAsync(session);
        session.Pause();
        PausedEventArgs paused = await pausedTask;

        paused.Reason.ShouldBe(PauseReason.ManualPause);
        session.Status.ShouldBe(SessionStatus.Paused);

        session.Stop();
    }
}
