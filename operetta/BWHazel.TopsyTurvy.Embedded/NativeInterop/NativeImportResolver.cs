using System;
using System.Runtime.InteropServices;
using System.Text;
using BWHazel.TopsyTurvy.Runtime;

namespace BWHazel.TopsyTurvy.Embedded.NativeInterop;

/// <summary>
/// Bridges a session-registered <c>resolve_import</c> callback to a delegate usable by the interpreter.
/// </summary>
/// <remarks>
/// The callback returns a caller-owned buffer: the returned string is copied out immediately within the
/// callback synchronous stack frame and the pointer is never retained, freed, or accessed afterwards.
/// Ownership of the buffer stays with the caller throughout.
/// </remarks>
public static unsafe class NativeImportResolver
{
    /// <summary>
    /// The maximum UTF-8 byte length to encode via <c>stackalloc</c> rather than a heap-allocated array.
    /// </summary>
    /// <remarks>
    /// Chosen to stay well within a thread's default stack size while still covering typical import
    /// filenames without a heap allocation; anything longer falls back to a heap array so an unusually
    /// long filename cannot risk a stack overflow.
    /// </remarks>
    private const int MaxStackAllocBytes = 512;

    /// <summary>
    /// Creates a delegate that resolves an import filename via the session <c>resolve_import</c> callback.
    /// </summary>
    /// <param name="callbacks">The session-registered callbacks.</param>
    /// <returns>A delegate suitable for <see cref="InterpreterExecutionOptions.SourceFileResolver"/>.</returns>
    public static Func<string, string?> Create(NativeCallbacks callbacks) =>
        filename =>
        {
            int byteCount = Encoding.UTF8.GetByteCount(filename);
            Span<byte> buffer = byteCount < MaxStackAllocBytes
                ? stackalloc byte[byteCount + 1]
                : new byte[byteCount + 1];

            Encoding.UTF8.GetBytes(filename, buffer);
            buffer[byteCount] = 0;

            fixed (byte* filenamePointer = buffer)
            {
                byte* resultPointer = callbacks.ResolveImport(callbacks.Context, filenamePointer);
                return resultPointer is null
                    ? null
                    : Marshal.PtrToStringUTF8((nint)resultPointer);
            }
        };
}
