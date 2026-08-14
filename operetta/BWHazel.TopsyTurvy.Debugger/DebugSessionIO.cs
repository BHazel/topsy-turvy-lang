using System;
using BWHazel.TopsyTurvy.Sdk.Interop.IO;

namespace BWHazel.TopsyTurvy.Debugger;

/// <summary>
/// Provides input and output for Topsy Turvy programs where the output is handled by the supplied delegate.
/// </summary>
/// <remarks>
/// Interactive input during a debug session is not supported, delegating <see cref="ReadLine"/>
/// straight to <see cref="Console.ReadLine"/>.
/// </remarks>
/// <param name="onWriteLine">The delegate invoked with every line the target programme writes.</param>
internal sealed class DebugSessionIO(Action<string> onWriteLine) : ITopsyTurvyIO
{
    /// <inheritdoc/>
    public string ReadLine() => Console.ReadLine() ?? string.Empty;

    /// <inheritdoc/>
    public void WriteLine(string message, bool suppressNewline = false) => onWriteLine(message);
}
