using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using BWHazel.TopsyTurvy.Sdk.Interop.IO;

namespace BWHazel.TopsyTurvy.Embedded.NativeInterop;

/// <summary>
/// Provides callback-backed input and output for a Topsy Turvy programme running inside a native session.
/// </summary>
/// <remarks>
/// <para>
/// Both <see cref="ReadLine"/> and <see cref="WriteLine"/> callbacks are invoked on the same thread that
/// called <see cref="NativeExports.ToolchainExports.ExecuteProgramme"/>.
/// </para>
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
    /// Reads the next line of input, following the three-step chain documented on this class.
    /// </summary>
    /// <remarks>
    /// Input tries three sources in order:
    /// <list type="bullet">
    /// <item>The pre-populated input queue supplied before execution.</item>
    /// <item>The session-registered <c>input_line</c> callback, if one was registered.</item>
    /// <item>An empty string.</item>
    /// </list>
    /// </remarks>
    /// <returns>
    /// The next pre-supplied input line; otherwise the result of the <c>input_line</c> callback, if
    /// registered and it returned a non-null buffer; otherwise an empty string.
    /// </returns>
    public string ReadLine()
    {
        if (this.inputQueue.Count > 0)
        {
            return this.inputQueue.Dequeue();
        }

        if (this.callbacks.InputLine is not null)
        {
            byte* resultPointer = this.callbacks.InputLine(this.callbacks.Context);
            if (resultPointer is not null)
            {
                return Marshal.PtrToStringUTF8((nint)resultPointer) ?? string.Empty;
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Invokes the session-registered <c>output_line</c> callback with the given message.
    /// </summary>
    /// <remarks>
    /// Output invokes the session-registered <c>output_line</c> callback synchronously for each line
    /// written by the programme, in order.
    /// </remarks>
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
