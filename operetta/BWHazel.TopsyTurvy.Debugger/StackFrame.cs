using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Runtime;

namespace BWHazel.TopsyTurvy.Debugger;

/// <summary>
/// An immutable snapshot of one active function call, taken at the moment a debug session pauses.
/// </summary>
/// <param name="Id">The identity of the frame for this pause.</param>
/// <param name="Name">The function name, or a fixed sentinel for the top-level frame.</param>
/// <param name="Span">The current paused or waiting source location of the frame.</param>
/// <param name="Environment">The environment this frame executes in.</param>
/// <remarks>
/// A paused session has one <see cref="StackFrame"/> per active call, ordered from the most recent call to the
/// original entry point, built purely from <see cref="Runtime.IExecutionObserver.OnFunctionEnter"/> and
/// <see cref="Runtime.IExecutionObserver.OnFunctionExit"/>, never read from the CLR call stack.
/// </remarks>
public sealed record StackFrame(int Id, string Name, SourceSpan Span, TopsyTurvyEnvironment Environment)
{
    /// <summary>
    /// Gets the environment this frame executes in.
    /// </summary>
    /// <remarks>
    /// Redeclared here to narrow the compiler-generated positional property from <c>public</c> to <c>internal</c>:
    /// this is never serialised to the debugger front end, only walked internally via
    /// <see cref="TopsyTurvyEnvironment.Enclosing"/> to build the labelled scopes of the frame.
    /// </remarks>
    internal TopsyTurvyEnvironment Environment { get; init; } = Environment;
}
