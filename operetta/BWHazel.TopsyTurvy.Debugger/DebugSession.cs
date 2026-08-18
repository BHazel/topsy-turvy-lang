using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Parser;
using BWHazel.TopsyTurvy.Runtime;
using BWHazel.TopsyTurvy.TypeChecker;
using Superpower;
using Superpower.Model;

namespace BWHazel.TopsyTurvy.Debugger;

/// <summary>
/// Orchestrates a single debugging session for one Topsy Turvy programme.
/// </summary>
/// <remarks>
/// <para>
/// This is the shared, protocol-agnostic debugging engine bound to directly by debugger front-ends.
/// </para>
/// <para>
/// <see cref="StartAsync"/> parses and type-checks the target file and transitions straight to
/// <see cref="SessionStatus.Paused"/> without running any statement, so that setting a breakpoint before the first
/// <see cref="Continue"/>, <see cref="StepOver"/>, <see cref="StepIn"/> or <see cref="StepOut"/> call is guaranteed to be
/// in place before any code executes. The interpreter for the target programme does not actually start running
/// until the first such call.
/// </para>
/// </remarks>
/// <param name="sourceFileResolver">
/// A resolver used both to read the target file and to resolve any <c>PRAY ADMIT</c> imports it contains, or
/// <c>null</c> to read from the file system.
/// </param>
public sealed class DebugSession(Func<string, string?>? sourceFileResolver = null)
{
    private readonly Func<string, string?>? sourceFileResolver = sourceFileResolver;
    private string? sourceFilePath;

    private readonly CancellationTokenSource cancellationTokenSource = new();
    private CancellationToken linkedCancellationToken;

    private readonly TopsyTurvyParser parser = new();
    private readonly TopsyTurvyTypeChecker typeChecker = new();
    private string[] sourceLines = [];
    private DebuggerExecutionObserver? debuggerExecutionObserver;
    private Interpreter? interpreter;
    private InterpreterExecutionOptions? executionOptions;
    private ProgramNode? program;
    private Task? executionTask;
    private volatile bool isStoppedDeliberately;

    /// <summary>
    /// Gets the current lifecycle status of the session.
    /// </summary>
    public SessionStatus Status { get; private set; } = SessionStatus.NotStarted;

    /// <summary>
    /// Gets the path to the target file being debugged, or <c>null</c> before <see cref="StartAsync"/> has run.
    /// </summary>
    /// <remarks>
    /// Every reported <see cref="StackFrame"/> belongs to this one file.  An opaque frame for an imported file
    /// via <c>PRAY ADMIT</c> never pauses, so this file is the only one a paused call stack ever needs to point a client at.
    /// </remarks>
    public string? SourceFilePath => this.sourceFilePath;

    /// <summary>
    /// Gets the reason execution most recently paused, or <c>null</c> if it has never paused.
    /// </summary>
    /// <remarks>
    /// When started the session immediately enters the <see cref="SessionStatus.Paused"/> state, before the first resume call.
    /// </remarks>
    public PauseReason? LastPauseReason { get; private set; }

    /// <summary>
    /// Raised every time execution transitions into the <see cref="SessionStatus.Paused"/> state for a reason.
    /// </summary>
    /// <remarks>
    /// Reasons can include a breakpoint, a completed step, an unhandled error or a manual pause.
    /// </remarks>
    public event EventHandler<PausedEventArgs>? Paused;

    /// <summary>
    /// Raised for every line the target programme writes to standard output.
    /// </summary>
    public event EventHandler<OutputEventArgs>? Output;

    /// <summary>
    /// Raised once when the session reaches the <see cref="SessionStatus.Ended"/> state.
    /// </summary>
    public event EventHandler<EndedEventArgs>? Ended;

    /// <summary>
    /// Parses and type-checks the target file and prepares the session to run it.
    /// </summary>
    /// <param name="sourceFilePath">The path to the Topsy Turvy file to debug.</param>
    /// <param name="cancellationToken">A token that, when cancelled, has the same effect as calling <see cref="Stop"/>.</param>
    /// <returns>A completed task once the session is ready and <see cref="Status"/> is <see cref="SessionStatus.Paused"/>.</returns>
    /// <exception cref="DebugSessionStartException">Thrown when the file cannot be read, fails to parse, or fails to type-check.</exception>
    public Task StartAsync(string sourceFilePath, CancellationToken cancellationToken)
    {
        string source = this.ReadSource(sourceFilePath);
        ProgramNode parsedProgram;
        try
        {
            parsedProgram = this.parser.Parse(source);
        }
        catch (TopsyTurvySyntaxException ex)
        {
            throw new DebugSessionStartException($"Syntax errors in '{sourceFilePath}': {string.Join("; ", ex.Errors)}");
        }

        TypeCheckResult typeCheckResult = this.typeChecker.Check(parsedProgram, this.sourceFileResolver);
        if (!typeCheckResult.Success)
        {
            string errors = string.Join("; ", typeCheckResult.Diagnostics.Select(diagnostic => diagnostic.Message));
            throw new DebugSessionStartException($"Type errors in '{sourceFilePath}': {errors}");
        }

        this.program = parsedProgram;
        this.sourceFilePath = sourceFilePath;
        this.sourceLines = source.ReplaceLineEndings("\n").Split('\n');
        this.executionOptions = new(
            ExecutionTimeout: null,
            SourceFilePath: sourceFilePath,
            SourceFileResolver: this.sourceFileResolver);
        this.linkedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.cancellationTokenSource.Token).Token;

        this.debuggerExecutionObserver = new(this.linkedCancellationToken);
        this.debuggerExecutionObserver.Paused += this.OnObserverPaused;
        this.interpreter = new(new DebugSessionIO(this.RaiseOutput), observer: this.debuggerExecutionObserver);

        this.Status = SessionStatus.Paused;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Ends the session from any status, stopping the target programme.
    /// </summary>
    public void Stop()
    {
        if (this.Status == SessionStatus.Ended)
        {
            return;
        }

        this.isStoppedDeliberately = true;
        this.cancellationTokenSource.Cancel();

        if (this.executionTask is null)
        {
            this.Status = SessionStatus.Ended;
            this.Ended?.Invoke(this, new(new(), 0));
            return;
        }

        if (this.Status == SessionStatus.Paused)
        {
            this.debuggerExecutionObserver!.Release();
        }
    }

    /// <summary>
    /// Sets a breakpoint at the given line.
    /// </summary>
    /// <remarks>
    /// The breakpoint is created with <see cref="Breakpoint.IsVerified"/> set to <c>false</c> if no statement
    /// starts on <paramref name="line"/>
    /// </remarks>
    /// <param name="line">The 1-based source line.</param>
    /// <returns>The created <see cref="Breakpoint"/>.</returns>
    public Breakpoint SetBreakpoint(int line)
    {
        this.EnsureStarted();
        return this.debuggerExecutionObserver!.SetBreakpoint(this.sourceFilePath!, line, this.program!);
    }

    /// <summary>
    /// Removes a previously set breakpoint.
    /// </summary>
    /// <remarks>
    /// If the breakpoint was not set, no action occurs.
    /// </remarks>
    /// <param name="breakpointId">The ID of the breakpoint.</param>
    public void RemoveBreakpoint(int breakpointId)
    {
        this.EnsureStarted();
        this.debuggerExecutionObserver!.RemoveBreakpoint(breakpointId);
    }

    /// <summary>
    /// Gets all active breakpoints.
    /// </summary>
    /// <returns>The active breakpoints.</returns>
    public IReadOnlyList<Breakpoint> GetBreakpoints()
    {
        this.EnsureStarted();
        return this.debuggerExecutionObserver!.GetBreakpoints();
    }

    /// <summary>
    /// Gets the source text of a single line of the file being debugged.
    /// </summary>
    /// <param name="line">The 1-based source line.</param>
    /// <returns>The text of the line, with no trailing line terminator.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="line"/> is outside the file.</exception>
    public string GetSourceLine(int line)
    {
        this.EnsureStarted();
        if (line < 1 || line > this.sourceLines.Length)
        {
            throw new ArgumentException($"Line {line} is outside '{this.sourceFilePath}'.", nameof(line));
        }

        return this.sourceLines[line - 1];
    }

    /// <summary>
    /// Resumes execution until the next breakpoint, an unhandled error, or completion.
    /// </summary>
    public void Continue() => this.Resume(observer => observer.PrepareContinue());

    /// <summary>
    /// Resumes execution, pausing again at the next statement in the current frame without descending into a call.
    /// </summary>
    public void StepOver() => this.Resume(observer => observer.PrepareStepOver());

    /// <summary>
    /// Resumes execution, pausing again at the very next statement, descending into the body of a called function
    /// if the next statement is a call.
    /// </summary>
    /// <remarks>
    /// If the call is to a function that came from a <c>PRAY ADMIT</c> import, this behaves like
    /// <see cref="StepOver"/> for that call.
    /// </remarks>
    public void StepIn() => this.Resume(observer => observer.PrepareStepIn());

    /// <summary>
    /// Resumes execution, pausing again once the current frame returns to its caller.
    /// </summary>
    public void StepOut() => this.Resume(observer => observer.PrepareStepOut());

    /// <summary>
    /// Requests a pause at the next statement boundary.
    /// </summary>
    /// <remarks>
    /// Valid only when <see cref="Status"/> is <see cref="SessionStatus.Running"/>.
    /// </remarks>
    public void Pause()
    {
        if (this.Status != SessionStatus.Running)
        {
            throw new InvalidOperationException($"Cannot pause: session status is {this.Status}, not {SessionStatus.Running}.");
        }

        this.debuggerExecutionObserver!.RequestManualPause();
    }

    /// <summary>
    /// Gets the current call stack, most-recent call first.
    /// </summary>
    /// <remarks>
    /// Valid only when <see cref="Status"/> is <see cref="SessionStatus.Paused"/>.
    /// </remarks>
    /// <returns>The current <see cref="StackFrame"/>s.</returns>
    public IReadOnlyList<StackFrame> GetCallStack()
    {
        this.EnsurePaused();
        return this.debuggerExecutionObserver!.SnapshotFrames();
    }

    /// <summary>
    /// Gets the labelled variable scopes visible in a paused frame.
    /// <remarks>
    /// Valid only when <see cref="Status"/> is <see cref="SessionStatus.Paused"/>.
    /// </remarks>
    /// </summary>
    /// <param name="frameId">The ID of the stack frame to inspect.</param>
    /// <returns>
    /// A <c>Locals</c> scope for every variable visible from <paramref name="frameId"/> and a
    /// <c>Globals</c> scope included only when that frame is the top-level frame, covering the variables
    /// declared directly in its outermost environment (<see cref="TopsyTurvyEnvironment.Enclosing"/> is <c>null</c>).
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="frameId"/> does not refer to a frame in the current call stack.</exception>
    public IReadOnlyList<VariableScope> GetVariables(int frameId)
    {
        StackFrame frame = this.FindFrame(frameId);

        Dictionary<string, TopsyTurvyValue> localVariables = [];
        Dictionary<string, TopsyTurvyValue>? globalVariables = null;
        bool isTopLevelFrame = frame.Name == DebuggerExecutionObserver.TopLevelFrameName;

        TopsyTurvyEnvironment? currentEnvironment = frame.Environment;
        while (currentEnvironment is not null)
        {
            bool isOutermost = currentEnvironment.Enclosing is null;
            Dictionary<string, TopsyTurvyValue> targetVariableScope = isOutermost && isTopLevelFrame
                ? globalVariables ??= []
                : localVariables;

            foreach (KeyValuePair<string, TopsyTurvyValue> variable in currentEnvironment.GetVariables())
            {
                targetVariableScope.TryAdd(variable.Key, variable.Value);
            }

            currentEnvironment = currentEnvironment.Enclosing;
        }

        List<VariableScope> scopes = [new("Locals", localVariables)];
        if (globalVariables is not null)
        {
            scopes.Add(new("Globals", globalVariables));
        }

        return scopes;
    }

    /// <summary>
    /// Evaluates an expression against the variables of a paused frame.
    /// </summary>
    /// <param name="expressionText">The expression to parse and evaluate.</param>
    /// <param name="frameId">The ID of the stack frame whose environment the expression is evaluated against.</param>
    /// <remarks>
    /// Valid only when <see cref="Status"/> is <see cref="SessionStatus.Paused"/>.
    /// </remarks>
    /// <returns>The evaluation outcome; a parse or evaluation failure is reported here, not thrown.</returns>
    public EvaluationResult Evaluate(string expressionText, int frameId)
    {
        StackFrame stackFrame = this.FindFrame(frameId);
        Result<Expression> parseResult = ExpressionParser.Expression.TryParse(expressionText);
        if (!parseResult.HasValue)
        {
            return EvaluationResult.Failed(parseResult.ToString());
        }

        try
        {
            TopsyTurvyValue value = this.interpreter!.EvaluateExpression(parseResult.Value, stackFrame.Environment);
            return EvaluationResult.Ok(value);
        }
        catch (TopsyTurvyRuntimeException ex)
        {
            return EvaluationResult.Failed(ex.Message);
        }
    }

    /// <summary>
    /// Reads the source text of the target file.
    /// </summary>
    /// <param name="sourceFilePath">The path to read.</param>
    /// <returns>The source text of the file.</returns>
    /// <exception cref="DebugSessionStartException">Thrown when the file cannot be read.</exception>
    private string ReadSource(string sourceFilePath)
    {
        if (this.sourceFileResolver is not null)
        {
            return this.sourceFileResolver(sourceFilePath)
                ?? throw new DebugSessionStartException($"Cannot resolve '{sourceFilePath}'.");
        }

        try
        {
            return File.ReadAllText(sourceFilePath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new DebugSessionStartException($"Cannot read '{sourceFilePath}': {ex.Message}");
        }
    }

    /// <summary>
    /// Prepares the given step mode and either starts the target programme running for the first time, or releases
    /// an already-blocked interpreter thread to resume under it.
    /// </summary>
    /// <param name="prepare">Sets the desired step mode on the debugger execution observer before resuming.</param>
    private void Resume(Action<DebuggerExecutionObserver> prepare)
    {
        this.EnsurePaused();
        prepare(this.debuggerExecutionObserver!);
        this.Status = SessionStatus.Running;

        if (this.executionTask is null)
        {
            this.executionTask = Task.Run(this.RunInterpreter);
        }
        else
        {
            this.debuggerExecutionObserver!.Release();
        }
    }

    /// <summary>
    /// Runs the target programme to completion, or until it pauses and is later stopped.
    /// </summary>
    /// <remarks>
    /// When <see cref="Stop"/> causes this run to end, the diagnostics from the <see cref="Interpreter"/> report the
    /// cancellation as "Execution timed out.", accurate for the original timeout-support purpose of that message,
    /// but misleading for a deliberate developer-requested stop. That diagnostic is replaced with an empty
    /// collection here.
    /// </remarks>
    private void RunInterpreter()
    {
        DiagnosticCollection diagnostics = this.interpreter!.Execute(this.program!, this.linkedCancellationToken, this.executionOptions);
        this.Status = SessionStatus.Ended;
        this.Ended?.Invoke(this, new(
            this.isStoppedDeliberately
                ? new()
                : diagnostics,
            this.interpreter.ExitCode));
    }

    /// <summary>
    /// Handles the <see cref="DebuggerExecutionObserver.Paused"/> event from the observer by updating session state
    /// and re-raising it as <see cref="Paused"/>.
    /// </summary>
    private void OnObserverPaused(object? sender, PausedEventArgs eventArgs)
    {
        this.Status = SessionStatus.Paused;
        this.LastPauseReason = eventArgs.Reason;
        this.Paused?.Invoke(this, eventArgs);
    }

    /// <summary>
    /// Raises <see cref="Output"/> for a line the target programme writes to standard output.
    /// </summary>
    /// <param name="text">The line written to standard output.</param>
    private void RaiseOutput(string text) => this.Output?.Invoke(this, new(text));

    /// <summary>
    /// Finds a frame by ID in the current call stack.
    /// </summary>
    /// <param name="frameId">The ID of the stack frame to find.</param>
    /// <remarks>
    /// Requires <see cref="Status"/> to be <see cref="SessionStatus.Paused"/>.
    /// </remarks>
    /// <returns>The matching frame.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="frameId"/> does not refer to a frame in the current call stack.</exception>
    private StackFrame FindFrame(int frameId)
    {
        this.EnsurePaused();
        return this.debuggerExecutionObserver!.SnapshotFrames().FirstOrDefault(frame => frame.Id == frameId)
            ?? throw new ArgumentException($"No frame with id {frameId} in the current call stack.", nameof(frameId));
    }

    /// <summary>
    /// Ensures the session has been started.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the session has not started.</exception>
    private void EnsureStarted()
    {
        if (this.program is null)
        {
            throw new InvalidOperationException("Cannot perform this operation before the session has started.");
        }
    }

    /// <summary>
    /// Ensures the session is paused.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the session is not paused.</exception>
    private void EnsurePaused()
    {
        if (this.Status != SessionStatus.Paused)
        {
            throw new InvalidOperationException($"This operation requires the session to be {SessionStatus.Paused}, but it is {this.Status}.");
        }
    }
}
