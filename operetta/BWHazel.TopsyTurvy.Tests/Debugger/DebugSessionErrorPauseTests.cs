using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Debugger;

namespace BWHazel.TopsyTurvy.Tests.Debugger;

/// <summary>
/// Pause-on-unhandled-error tests for the <see cref="DebugSession"/> class.
/// </summary>
public class DebugSessionErrorPauseTests
{
    /// <summary>
    /// Tests that the <see cref="DebugSession"/> pauses with <see cref="PauseReason.UnhandledError"/> at the
    /// statement that raised the error, instead of ending the session immediately, when no breakpoint is set.
    /// </summary>
    [Fact]
    public async Task Continue_WithNoBreakpointsAndUnhandledError_PausesWithUnhandledErrorReason()
    {
        const string fileName = "error.topsy";
        string source = """
            HARK! "Error"
            A HIDEOUS CURSE ON "boom"
            FINALE.
            """;

        Dictionary<string, string> files = new()
        {
            [fileName] = source
        };

        DebugSession session = new(name => files.GetValueOrDefault(name));
        await session.StartAsync(fileName, CancellationToken.None);
        Task<PausedEventArgs> pausedTask = TestDebugPrograms.PreparePausedWaitAsync(session);
        
        session.Continue();
        
        PausedEventArgs paused = await pausedTask;
        paused.Reason.ShouldBe(PauseReason.UnhandledError);
        session.Status.ShouldBe(SessionStatus.Paused);
        session.GetCallStack()[0].Span.Start.Line.ShouldBe(2);
    }

    /// <summary>
    /// Tests that the call stack and in-scope variables are still available for inspection while paused on an
    /// unhandled error raised deep inside a nested function call.
    /// </summary>
    [Fact]
    public async Task Continue_WithErrorInsideNestedFunctionCall_CallStackAndVariablesRemainInspectable()
    {
        const string fileName = "nestederror.topsy";
        string source = """
            HARK! "NestedError"
            IT IS MY DUTY TO PERFORM Inner UNDER NO OBLIGATION
              PRAY WELCOME innerValue AS A PEER BEING 7
              A HIDEOUS CURSE ON "boom"
            MY DUTY IS DISCHARGED.
            IT IS MY DUTY TO PERFORM Outer UNDER NO OBLIGATION
              SUMMON Inner WITH NOTHING IF YOU PLEASE.
            MY DUTY IS DISCHARGED.
            SUMMON Outer WITH NOTHING IF YOU PLEASE.
            FINALE.
            """;

        Dictionary<string, string> files = new()
        {
            [fileName] = source
        };

        DebugSession session = new(name => files.GetValueOrDefault(name));
        await session.StartAsync(fileName, CancellationToken.None);
        Task<PausedEventArgs> pausedTask = TestDebugPrograms.PreparePausedWaitAsync(session);
        
        session.Continue();
        
        PausedEventArgs paused = await pausedTask;
        paused.Reason.ShouldBe(PauseReason.UnhandledError);
        session.GetCallStack().Count.ShouldBe(3);
        session.GetCallStack()[0].Name.ShouldBe("Inner");
        session.GetCallStack()[1].Name.ShouldBe("Outer");

        int innerFrameId = session.GetCallStack()[0].Id;
        session.GetVariables(innerFrameId)[0].Variables["innerValue"].RawValue.ShouldBe(7);
    }

    /// <summary>
    /// Tests that the observer notifies the pause exactly once for an error that unwinds through multiple nested
    /// function calls, not once per stack level.
    /// </summary>
    [Fact]
    public async Task Continue_WithErrorUnwindingThroughNestedCalls_PausesExactlyOnce()
    {
        const string fileName = "onceerror.topsy";
        string source = """
            HARK! "OnceError"
            IT IS MY DUTY TO PERFORM Inner UNDER NO OBLIGATION
              A HIDEOUS CURSE ON "boom"
            MY DUTY IS DISCHARGED.
            IT IS MY DUTY TO PERFORM Outer UNDER NO OBLIGATION
              SUMMON Inner WITH NOTHING IF YOU PLEASE.
            MY DUTY IS DISCHARGED.
            SUMMON Outer WITH NOTHING IF YOU PLEASE.
            FINALE.
            """;

        Dictionary<string, string> files = new()
        {
            [fileName] = source
        };

        DebugSession session = new(name => files.GetValueOrDefault(name));
        await session.StartAsync(fileName, CancellationToken.None);
        int pauseCount = 0;
        session.Paused += (_, _) => pauseCount++;
        Task<PausedEventArgs> pausedTask = TestDebugPrograms.PreparePausedWaitAsync(session);

        session.Continue();
        
        await pausedTask;
        pauseCount.ShouldBe(1);

        Task<EndedEventArgs> endedTask = TestDebugPrograms.PrepareEndedWaitAsync(session);
        session.Continue();
        await endedTask;

        pauseCount.ShouldBe(1);
    }

    /// <summary>
    /// Tests that once the paused inspection ends the session reports the error in the same form a non-debugged
    /// <c>perform</c> run would: a runtime-error diagnostic, not a clean success.
    /// </summary>
    [Fact]
    public async Task Ended_AfterUnhandledErrorPauseIsResumed_ReportsRuntimeErrorDiagnostic()
    {
        const string fileName = "reporterror.topsy";
        string source = """
            HARK! "ReportError"
            A HIDEOUS CURSE ON "boom"
            FINALE.
            """;

        Dictionary<string, string> files = new()
        {
            [fileName] = source
        };

        DebugSession session = new(name => files.GetValueOrDefault(name));
        await session.StartAsync(fileName, CancellationToken.None);
        Task<PausedEventArgs> pausedTask = TestDebugPrograms.PreparePausedWaitAsync(session);
        session.Continue();
        await pausedTask;
        Task<EndedEventArgs> endedTask = TestDebugPrograms.PrepareEndedWaitAsync(session);
        session.Continue();
        
        EndedEventArgs ended = await endedTask;

        ended.Diagnostics.HasErrors.ShouldBeTrue();
    }
}
