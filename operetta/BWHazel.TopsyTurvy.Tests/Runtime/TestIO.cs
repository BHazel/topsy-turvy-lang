using System.Collections.Generic;
using BWHazel.TopsyTurvy.Sdk.Interop.IO;

namespace BWHazel.TopsyTurvy.Tests.Runtime;

/// <summary>
/// In-memory implementation of <see cref="ITopsyTurvyIO"/> for testing.
/// </summary>
internal sealed class TestIO : ITopsyTurvyIO
{
    private readonly List<string> output;
    private readonly Queue<string> input;

    /// <summary>
    /// Initialises a new instance of the <see cref="TestIO"/> class.
    /// </summary>
    /// <param name="output">The list that receives written lines for output.</param>
    /// <param name="input">The queue from which input lines are served.</param>
    internal TestIO(List<string> output, Queue<string> input)
    {
        this.output = output;
        this.input = input;
    }

    /// <summary>
    /// Gets the writes recorded by <see cref="WriteLine"/>, in order, including the <c>suppressNewline</c> flag of each one.
    /// </summary>
    internal List<(string Message, bool SuppressNewline)> Writes { get; } = [];

    /// <inheritdoc/>
    public string ReadLine() => this.input.Dequeue();

    /// <inheritdoc/>
    public void WriteLine(string message, bool suppressNewline = false)
    {
        this.output.Add(message);
        this.Writes.Add((message, suppressNewline));
    }
}
