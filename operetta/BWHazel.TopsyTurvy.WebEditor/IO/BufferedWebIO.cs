using System.Collections.Generic;
using BWHazel.TopsyTurvy.Sdk.Interop.IO;

namespace BWHazel.TopsyTurvy.WebEditor.IO;

/// <summary>
/// Provides buffered browser-based input and output for Topsy Turvy programs.
/// </summary>
/// <remarks>
/// Input is read from a pre-populated queue of lines supplied before execution.
/// Output lines are collected in a list for rendering to the terminal after execution.
/// </remarks>
public sealed class BufferedWebIO : ITopsyTurvyIO
{
    private readonly Queue<string> inputQueue;
    private readonly List<string> outputLines = [];

    /// <summary>
    /// Initialises a new instance of the <see cref="BufferedWebIO"/> class.
    /// </summary>
    /// <param name="preSuppliedInput">The lines of input to supply to the program, in order.</param>
    public BufferedWebIO(IEnumerable<string> preSuppliedInput)
    {
        this.inputQueue = new(preSuppliedInput);
    }

    /// <summary>
    /// Gets the output lines written by the program during execution.
    /// </summary>
    public IReadOnlyList<string> OutputLines => this.outputLines;

    /// <summary>
    /// Dequeues and returns the next pre-supplied input line.
    /// </summary>
    /// <returns>The next input line, or an empty string if no more input is available.</returns>
    public string ReadLine()
    {
        return this.inputQueue.Count > 0
            ? this.inputQueue.Dequeue()
            : string.Empty;
    }

    /// <summary>
    /// Appends a line to the output collection.
    /// </summary>
    /// <param name="message">The text to write.</param>
    /// <param name="suppressNewline">A value indicating whether to append the message to the last output line instead of starting a new one.</param>
    public void WriteLine(string message, bool suppressNewline = false)
    {
        if (suppressNewline && this.outputLines.Count > 0)
        {
            this.outputLines[this.outputLines.Count - 1] += message;
        }
        else
        {
            this.outputLines.Add(message);
        }
    }
}
