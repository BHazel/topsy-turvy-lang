using System;
using System.Collections.Generic;
using System.Text;
using BWHazel.TopsyTurvy.Runtime;

namespace BWHazel.TopsyTurvy.Embedded.NativeInterop;

/// <summary>
/// Provides callback-backed input and output for a Topsy Turvy programme running inside a native session.
/// </summary>
/// <remarks>
/// Input is read from a pre-populated input queue of lines supplied before execution.
/// Output invokes the session-registered <c>output_line</c> callback synchronously for each line written by the
/// programme, in order. The callback is invoked on the same thread that called <see cref="NativeExports.ExecuteProgramme"/>.
/// </remarks>
/// <param name="preSuppliedInput">The lines of input to supply to the programme, in order.</param>
/// <param name="callbacks">The session-registered callbacks.</param>
public sealed unsafe class BufferedNativeIO(IEnumerable<string> preSuppliedInput, NativeCallbacks callbacks) : ITopsyTurvyIO
{
    /// <summary>
    /// The maximum UTF-8 byte length to encode via <c>stackalloc</c> rather than a heap-allocated array.
    /// </summary>
    /// <remarks>
    /// Chosen to stay well within a thread's default stack size while still covering typical output lines
    /// without a heap allocation; anything longer falls back to a heap array so an unusually long line
    /// cannot risk a stack overflow.
    /// </remarks>
    private const int MaxStackAllocBytes = 512;

    private readonly Queue<string> inputQueue = new(preSuppliedInput);
    private readonly NativeCallbacks callbacks = callbacks;

    /// <summary>
    /// Dequeues and returns the next pre-supplied input line.
    /// </summary>
    /// <returns>The next input line, or an empty string if no more input is available.</returns>
    public string ReadLine() =>
        this.inputQueue.Count > 0
            ? this.inputQueue.Dequeue()
            : string.Empty;

    /// <summary>
    /// Invokes the session-registered <c>output_line</c> callback with the given message.
    /// </summary>
    /// <param name="message">The text to write.</param>
    /// <param name="suppressNewline">A value indicating whether the trailing newline is omitted.</param>
    public void WriteLine(string message, bool suppressNewline = false)
    {
        int byteCount = Encoding.UTF8.GetByteCount(message);
        Span<byte> buffer = byteCount < MaxStackAllocBytes
            ? stackalloc byte[byteCount + 1]
            : new byte[byteCount + 1];

        Encoding.UTF8.GetBytes(message, buffer);
        buffer[byteCount] = 0;

        fixed (byte* messagePointer = buffer)
        {
            this.callbacks.OutputLine(this.callbacks.Context, messagePointer, suppressNewline ? (byte)1 : (byte)0);
        }
    }
}
