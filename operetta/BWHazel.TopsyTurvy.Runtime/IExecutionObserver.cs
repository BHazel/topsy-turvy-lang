using System;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Runtime;

/// <summary>
/// Defines methods to observe the execution of a Topsy Turvy programme by the <see cref="Interpreter"/>.
/// </summary>
/// <remarks>
/// <para>
/// An <see cref="Interpreter"/> can be given an optional observer that is notified at four points during execution:
/// <list type="bullet">
/// <item><description>Immediately before each statement runs.</description></item>
/// <item><description>When a function call's body is about to begin executing.</description></item>
/// <item><description>When that body finishes executing.</description></item>
/// <item><description>When an unhandled runtime error is about to propagate out of the statement that raised it.</description></item>
/// </list>
/// This is the sole seam a debugger attaches to; the interpreter itself has no knowledge of breakpoints, stepping,
/// or pausing.
/// </para>
/// <para>
/// Implementations must not throw.  An observer is expected to record state or block the calling thread, for example
/// while waiting for a developer to resume a paused debug session.  It is not expected to alter execution itself.
/// </para>
/// </remarks>
public interface IExecutionObserver
{
    /// <summary>
    /// Called immediately before a statement is executed.
    /// </summary>
    /// <param name="statement">The statement about to execute.</param>
    /// <param name="environment">The environment the statement will execute in.</param>
    void OnBeforeStatement(Statement statement, TopsyTurvyEnvironment environment);

    /// <summary>
    /// Called when a function call is about to begin executing its body.
    /// </summary>
    /// <param name="functionName">The name the function was called by.</param>
    /// <param name="callSite">The source span of the call expression.</param>
    /// <param name="environment">The new environment created for the call.</param>
    /// <param name="isImportedFunction">
    /// <c>true</c> when the function's body was declared in a file imported via <c>PRAY ADMIT</c>, rather
    /// than the running programme's own source, otherwise <c>false</c>.
    /// </param>
    void OnFunctionEnter(string functionName, SourceSpan callSite, TopsyTurvyEnvironment environment, bool isImportedFunction);

    /// <summary>
    /// Called after a function call's body has finished executing, whether it returned normally or via an exception.
    /// </summary>
    /// <param name="functionName">The name the function was called by.</param>
    void OnFunctionExit(string functionName);

    /// <summary>
    /// Called when a runtime error is about to propagate out of the statement that raised it, before any enclosing
    /// call frame has started to unwind.
    /// </summary>
    /// <param name="exception">The exception that was raised.</param>
    void OnUnhandledError(Exception exception);
}
