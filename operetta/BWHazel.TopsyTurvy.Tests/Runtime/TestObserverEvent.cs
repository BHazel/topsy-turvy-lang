namespace BWHazel.TopsyTurvy.Tests.Runtime;

/// <summary>
/// A single entry in the ordered log <see cref="TestExecutionObserver"/> builds as the interpreter calls it.
/// </summary>
/// <param name="Kind">The callback that fired.</param>
/// <param name="Detail">The callback-specific detail string used for test assertions.</param>
/// <remarks>
/// One entry per callback invocation, letting a test assert both which callbacks fired and in what order.
/// </remarks>
internal sealed record TestObserverEvent(string Kind, string Detail);
