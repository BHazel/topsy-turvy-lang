using System;

namespace BWHazel.TopsyTurvy.Debugger;

/// <summary>
/// Thrown by <see cref="DebugSession.StartAsync"/> when the target file fails to parse or type-check.
/// </summary>
/// <param name="message">A description of the parse or type-check failure.</param>
public sealed class DebugSessionStartException(string message) : Exception(message)
{
}
