using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Runtime;

namespace BWHazel.TopsyTurvy.Debugger;

/// <summary>
/// A mutable, in-progress tracking for one active call frame, updated as execution proceeds within it.
/// </summary>
/// <remarks>
/// Distinct from the immutable <see cref="StackFrame"/> record: a <see cref="DebugFrameState"/> is the live frame
/// <see cref="DebuggerExecutionObserver"/> maintains across many statements, while a <see cref="StackFrame"/> is a
/// point-in-time snapshot taken when execution pauses.
/// </remarks>
/// <param name="id">The identity of this frame for the lifetime of the call.</param>
/// <param name="name">The function name, or the top-level sentinel.</param>
/// <param name="isOpaque">A value indicating whether the function of this frame came from a <c>PRAY ADMIT</c> import.</param>
internal sealed class DebugFrameState(int id, string name, bool isOpaque = false)
{
    /// <summary>
    /// Gets the identity of this frame for the lifetime of the call.
    /// </summary>
    internal int Id { get; } = id;

    /// <summary>
    /// Gets the function name, or the top-level sentinel.
    /// </summary>
    internal string Name { get; } = name;

    /// <summary>
    /// Gets a value indicating whether the function of this frame came from a <c>PRAY ADMIT</c> import.
    /// </summary>
    /// <remarks>
    /// Debugging of imported functions is currently not supported.
    /// </remarks>
    internal bool IsOpaque { get; } = isOpaque;

    /// <summary>
    /// Gets or sets the source location of the statement currently executing, or about to execute, in this frame.
    /// </summary>
    internal SourceSpan CurrentSpan { get; set; } = default!;

    /// <summary>
    /// Gets or sets the environment currently in effect in this frame.
    /// </summary>
    internal TopsyTurvyEnvironment CurrentEnvironment { get; set; } = default!;
}
