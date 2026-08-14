using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Runtime;

namespace BWHazel.TopsyTurvy.Debugger;

/// <summary>
/// Provide breakpoint, stepping, and call-stack tracking for a <see cref="DebugSession"/>.
/// </summary>
/// <remarks>
/// <para>
/// This owns every piece of state the pause/resume behaviour of a debug session depends on:
/// <list type="bullet">
/// <item><description>The active breakpoint set.</description></item>
/// <item><description>The current <see cref="StepMode"/> and its target call depth.</description></item>
/// <item><description>An explicit stack of <see cref="DebugFrameState"/> built purely from <see cref="OnFunctionEnter"/> and <see cref="OnFunctionExit"/> calls, never from the CLR call stack.</description></item>
/// </list>
/// </para>
/// <para>
/// When a pause condition is met, this observer raises <see cref="Paused"/> synchronously on the thread of the
/// interpreter and then blocks that thread on <see cref="resumeGate"/>, a semaphore only <see cref="Release"/> can
/// signal.  <see cref="DebugSession"/> resumes in two steps:
/// <list type="bullet">
/// <item><description>A "prepare" method (<see cref="PrepareContinue"/>, <see cref="PrepareStepOver"/>,
/// <see cref="PrepareStepIn"/> or <see cref="PrepareStepOut"/>) sets the mode to resume under.</description></item>
/// <item><description><see cref="Release"/> signals <see cref="resumeGate"/> to let the blocked thread continue.</description></item>
/// </list>
/// The split matters because the first "prepare" call happens before the target programme has started running,
/// when there is no thread blocked on <see cref="resumeGate"/> yet to release.
/// </para>
/// </remarks>
internal sealed class DebuggerExecutionObserver : IExecutionObserver
{
    /// <summary>
    /// The sentinel <see cref="StackFrame.Name"/> for the top-level frame, not inside any function.
    /// </summary>
    internal const string TopLevelFrameName = "<programme>";

    private readonly SemaphoreSlim resumeGate = new(0, 1);
    private readonly List<DebugFrameState> frameStack = [];
    private readonly Dictionary<int, Breakpoint> breakpoints = [];
    private readonly Lock breakpointsLock = new();
    private readonly CancellationToken cancellationToken;
    private int nextBreakpointId = 1;
    private int nextFrameId = 1;
    private StepMode stepMode = StepMode.None;
    private int stepTargetDepth;
    private bool manualPauseRequested;

    /// <summary>
    /// Initialises a new instance of the <see cref="DebuggerExecutionObserver"/> class.
    /// </summary>
    /// <param name="cancellationToken">The token that, once cancelled, causes a currently blocked (paused) thread to unwind promptly once released.</param>
    /// <remarks>
    /// The environment of the top-level frame is seeded with a fresh, empty <see cref="TopsyTurvyEnvironment"/> so that
    /// inspecting variables in the brief window between <see cref="DebugSession.StartAsync"/> returning and the
    /// first statement actually executing reports "nothing declared yet" rather than failing.  The real global
    /// environment the interpreter creates overwrites it on the first <see cref="OnBeforeStatement"/> call.
    /// </remarks>
    internal DebuggerExecutionObserver(CancellationToken cancellationToken)
    {
        this.cancellationToken = cancellationToken;
        DebugFrameState topLevelFrame = new(this.nextFrameId++, TopLevelFrameName)
        {
            CurrentEnvironment = TopsyTurvyEnvironment.CreateGlobal()
        };

        this.frameStack.Add(topLevelFrame);
    }

    /// <summary>
    /// Raised, synchronously on the thread of the interpreter, whenever execution pauses.
    /// </summary>
    internal event EventHandler<PausedEventArgs>? Paused;

    /// <inheritdoc/>
    public void OnBeforeStatement(Statement statement, TopsyTurvyEnvironment environment)
    {
        DebugFrameState currentFrame = this.frameStack[^1];
        currentFrame.CurrentSpan = statement.Span;
        currentFrame.CurrentEnvironment = environment;

        if (currentFrame.IsOpaque)
        {
            // Debugging of imported functions is not supported at this time.
            // The body of an imported function is opaque: no statement inside it may ever pause execution, for any
            // reason, including a breakpoint whose line number happens to collide with one in this frame.
            return;
        }

        if (this.manualPauseRequested)
        {
            this.manualPauseRequested = false;
            this.PauseAndBlock(PauseReason.ManualPause);
            return;
        }

        if (this.IsVerifiedBreakpointAt(statement.Span.Start.Line))
        {
            this.PauseAndBlock(PauseReason.BreakpointHit);
            return;
        }

        bool stepComplete = this.stepMode switch
        {
            StepMode.StepIn => true,
            StepMode.StepOver or StepMode.StepOut => this.frameStack.Count <= this.stepTargetDepth,
            _ => false
        };

        if (stepComplete)
        {
            this.PauseAndBlock(PauseReason.StepComplete);
        }
    }

    /// <inheritdoc/>
    public void OnFunctionEnter(string functionName, SourceSpan callSite, TopsyTurvyEnvironment environment, bool isImportedFunction)
    {
        DebugFrameState frame = new(this.nextFrameId++, functionName, isOpaque: isImportedFunction)
        {
            CurrentSpan = callSite,
            CurrentEnvironment = environment
        };

        this.frameStack.Add(frame);

        if (isImportedFunction && this.stepMode == StepMode.StepIn)
        {
            // Debugging of imported functions is not supported at this time.
            // Stepping into a PRAY ADMIT-imported function must behave like stepping over it: pause only once
            // control returns to the frame that made this call, never inside the body of the imported function.
            this.stepMode = StepMode.StepOver;
            this.stepTargetDepth = this.frameStack.Count - 1;
        }
    }

    /// <inheritdoc/>
    public void OnFunctionExit(string functionName) =>
        this.frameStack.RemoveAt(this.frameStack.Count - 1);

    /// <inheritdoc/>
    public void OnUnhandledError(Exception exception) =>
        this.PauseAndBlock(PauseReason.UnhandledError);

    /// <summary>
    /// Sets a breakpoint at the given line, marking it verified only if a statement in <paramref name="program"/> starts there.
    /// </summary>
    /// <param name="filePath">The source file of the session.</param>
    /// <param name="line">The 1-based source line.</param>
    /// <param name="program">The parsed programme, used to determine whether <paramref name="line"/> is reachable.</param>
    /// <returns>The created <see cref="Breakpoint"/>.</returns>
    internal Breakpoint SetBreakpoint(string filePath, int line, ProgramNode program)
    {
        bool isVerified = AllStatements(program.Statements).Any(statement => statement.Span.Start.Line == line);

        lock (this.breakpointsLock)
        {
            Breakpoint breakpoint = new(this.nextBreakpointId++, filePath, line, isVerified);
            this.breakpoints[breakpoint.Id] = breakpoint;
            return breakpoint;
        }
    }

    /// <summary>
    /// Removes a previously set breakpoint.
    /// </summary>
    /// <param name="breakpointId">The ID of the breakpoint.</param>
    internal void RemoveBreakpoint(int breakpointId)
    {
        lock (this.breakpointsLock)
        {
            this.breakpoints.Remove(breakpointId);
        }
    }

    /// <summary>
    /// Gets a snapshot of every active breakpoint.
    /// </summary>
    /// <returns>The active breakpoints.</returns>
    internal IReadOnlyList<Breakpoint> GetBreakpoints()
    {
        lock (this.breakpointsLock)
        {
            return [.. this.breakpoints.Values];
        }
    }

    /// <summary>
    /// Requests an on-demand pause at the next statement boundary.
    /// </summary>
    internal void RequestManualPause() => this.manualPauseRequested = true;

    /// <summary>
    /// Prepares unconditional resumption.
    /// </summary>
    /// <remarks>
    /// Runs until the next breakpoint, unhandled error, or completion.
    /// </remarks>
    internal void PrepareContinue() => this.stepMode = StepMode.None;

    /// <summary>
    /// Prepares a step-over.
    /// </summary>
    /// <remarks>
    /// Pause again at the next statement in the current frame without descending into a call.
    /// </remarks>
    internal void PrepareStepOver()
    {
        this.stepMode = StepMode.StepOver;
        this.stepTargetDepth = this.frameStack.Count;
    }

    /// <summary>
    /// Prepares a step-in.
    /// </summary>
    /// <remarks>
    /// Pause again at the very next statement, descending into a call if the next statement is one.
    /// </remarks>
    internal void PrepareStepIn() => this.stepMode = StepMode.StepIn;

    /// <summary>
    /// Prepares a step-out.
    /// </summary>
    /// <remarks>
    /// Pause again once the current frame returns to its caller.
    /// </remarks>
    internal void PrepareStepOut()
    {
        this.stepMode = StepMode.StepOut;
        this.stepTargetDepth = this.frameStack.Count - 1;
    }

    /// <summary>
    /// Releases a thread that is currently blocked in <see cref="PauseAndBlock"/>, letting it resume under whichever
    /// mode was most recently prepared.
    /// </summary>
    internal void Release() => this.resumeGate.Release();

    /// <summary>
    /// Gets an immutable snapshot of the current call stack, most-recent call first.
    /// </summary>
    /// <returns>The current <see cref="StackFrame"/>s.</returns>
    internal IReadOnlyList<StackFrame> SnapshotFrames() =>
        [.. this.frameStack
            .AsEnumerable()
            .Reverse()
            .Select(frame => new StackFrame(frame.Id, frame.Name, frame.CurrentSpan, frame.CurrentEnvironment))];

    /// <summary>
    /// Determines whether a verified breakpoint exists at the given line.
    /// </summary>
    /// <param name="line">The 1-based source line.</param>
    /// <returns><c>true</c> if a verified breakpoint is active at <paramref name="line"/>, otherwise <c>false</c>.</returns>
    private bool IsVerifiedBreakpointAt(int line)
    {
        lock (this.breakpointsLock)
        {
            return this.breakpoints.Values.Any(breakpoint => breakpoint.IsVerified && breakpoint.Line == line);
        }
    }

    /// <summary>
    /// Raises <see cref="Paused"/> and blocks the calling (interpreter) thread until released.
    /// </summary>
    /// <param name="reason">The reason execution is pausing.</param>
    private void PauseAndBlock(PauseReason reason)
    {
        this.stepMode = StepMode.None;
        int frameId = this.frameStack[^1].Id;
        this.Paused?.Invoke(this, new(reason, frameId));
        this.resumeGate.Wait();
        this.cancellationToken.ThrowIfCancellationRequested();
    }

    /// <summary>
    /// Recursively yields every statement in a block, including those nested inside conditionals, loops, switches,
    /// try-catch blocks, guards, and function bodies, so breakpoint verification (<see cref="SetBreakpoint"/>) can
    /// check reachability against the whole programme, not just its top-level statements.
    /// </summary>
    /// <param name="statements">The statement list to walk.</param>
    /// <returns>Every statement reachable from <paramref name="statements"/>, including <paramref name="statements"/> itself.</returns>
    private static IEnumerable<Statement> AllStatements(IEnumerable<Statement> statements)
    {
        foreach (Statement statement in statements)
        {
            yield return statement;

            IEnumerable<Statement> nestedStatements = statement switch
            {
                PrincipalBlockNode principalsBlock => AllStatements(principalsBlock.Declarations),
                FunctionDefinitionNode functionDefinition => AllStatements(functionDefinition.Body),
                ConditionalNode conditional =>
                    AllStatements(conditional.TrueBlock)
                        .Concat(conditional.ElseIfs.SelectMany(branch => AllStatements(branch.Block)))
                        .Concat(AllStatements(conditional.ElseBlock)),
                LoopNode loop => AllStatements(loop.Body),
                SwitchNode switchNode =>
                    switchNode.Cases.SelectMany(switchCase => AllStatements(switchCase.Block))
                        .Concat(AllStatements(switchNode.DefaultBlock)),
                TryCatchNode tryCatch => AllStatements(tryCatch.SuccessBlock)
                    .Concat(AllStatements(tryCatch.ExceptionBlock)),
                GuardNode guard => AllStatements(guard.ElseBlock),
                _ => []
            };

            foreach (Statement nestedStatement in nestedStatements)
            {
                yield return nestedStatement;
            }
        }
    }
}
