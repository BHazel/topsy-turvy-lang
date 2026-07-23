using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace BWHazel.TopsyTurvy.Tests.Embedded;

/// <summary>
/// Provides <see cref="UnmanagedCallersOnlyAttribute"/> callback targets for testing <c>tt_session_create</c>.
/// </summary>
/// <remarks>
/// Callback function pointers cannot capture instance state, so captured output is held in a static list
/// and the import-resolution/input-line results are each held in a static field.  Tests using this class
/// must call <see cref="Reset"/> before each session under test.
/// </remarks>
internal static unsafe class TestCallbackCapture
{
    private static readonly List<string> outputLines = [];
    private static string? importResolutionResult;
    private static string? inputLineResult;

    /// <summary>
    /// Gets the output lines captured by <see cref="OnOutputLine"/> since the last <see cref="Reset"/>.
    /// </summary>
    internal static IReadOnlyList<string> OutputLines => outputLines;

    /// <summary>
    /// Clears captured output, the import-resolution result and the input-line result.
    /// </summary>
    internal static void Reset()
    {
        outputLines.Clear();
        importResolutionResult = null;
        inputLineResult = null;
    }

    /// <summary>
    /// Sets the string returned by <see cref="OnResolveImport"/>, or <c>null</c> to simulate an unresolvable import.
    /// </summary>
    /// <param name="result">The import source text to return, or <c>null</c>.</param>
    internal static void SetImportResolutionResult(string? result) => importResolutionResult = result;

    /// <summary>
    /// Sets the string returned by <see cref="OnInputLine"/>, or <c>null</c> to simulate no input available.
    /// </summary>
    /// <param name="result">The input line to return, or <c>null</c>.</param>
    internal static void SetInputLineResult(string? result) => inputLineResult = result;

    /// <summary>
    /// Captures a single output line, appending to the previous line instead of starting a new one when suppressed.
    /// </summary>
    /// <param name="context">The opaque context pointer, unused by this test target.</param>
    /// <param name="utf8Message">A null-terminated, UTF-8 encoded pointer to the message.</param>
    /// <param name="suppressNewline">Non-zero when the message continues the previous output line.</param>
    [UnmanagedCallersOnly]
    internal static void OnOutputLine(nint context, byte* utf8Message, byte suppressNewline)
    {
        string message = Marshal.PtrToStringUTF8((nint)utf8Message) ?? string.Empty;
        if (suppressNewline != 0 && outputLines.Count > 0)
        {
            outputLines[^1] += message;
        }
        else
        {
            outputLines.Add(message);
        }
    }

    /// <summary>
    /// Returns the configured import-resolution result as a caller-owned UTF-8 buffer.
    /// </summary>
    /// <param name="context">The opaque context pointer, unused by this test target.</param>
    /// <param name="filenameUtf8">The requested import filename, unused by this test target.</param>
    /// <returns>A null-terminated, UTF-8 encoded buffer, or a null pointer if no result was configured.</returns>
    [UnmanagedCallersOnly]
    internal static byte* OnResolveImport(nint context, byte* filenameUtf8)
    {
        if (importResolutionResult is null)
        {
            return null;
        }

        int byteCount = Encoding.UTF8.GetByteCount(importResolutionResult);
        nint buffer = Marshal.AllocHGlobal(byteCount + 1);
        Span<byte> destination = new((void*)buffer, byteCount + 1);
        Encoding.UTF8.GetBytes(importResolutionResult, destination);
        destination[byteCount] = 0;
        return (byte*)buffer;
    }

    /// <summary>
    /// Returns the configured input-line result as a caller-owned UTF-8 buffer.
    /// </summary>
    /// <param name="context">The opaque context pointer, unused by this test target.</param>
    /// <returns>A null-terminated, UTF-8 encoded buffer, or a null pointer if no result was configured.</returns>
    [UnmanagedCallersOnly]
    internal static byte* OnInputLine(nint context)
    {
        if (inputLineResult is null)
        {
            return null;
        }

        int byteCount = Encoding.UTF8.GetByteCount(inputLineResult);
        nint buffer = Marshal.AllocHGlobal(byteCount + 1);
        Span<byte> destination = new((void*)buffer, byteCount + 1);
        Encoding.UTF8.GetBytes(inputLineResult, destination);
        destination[byteCount] = 0;
        return (byte*)buffer;
    }
}
