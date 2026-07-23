using System;
using System.Runtime.InteropServices;
using System.Text;
using BWHazel.TopsyTurvy.Embedded.NativeInterop;

namespace BWHazel.TopsyTurvy.Embedded.NativeExports;

/// <summary>
/// Support helpers for types exporting <see cref="System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute"/>-annotated
/// methods.
/// </summary>
internal static unsafe class NativeExportSupport
{
    /// <summary>
    /// Resolves a session handle to its <see cref="NativeSession"/>, treating any failure as an invalid handle.
    /// </summary>
    /// <param name="handle">The session handle to resolve.</param>
    /// <param name="session">The resolved session, or <c>null</c> if resolution failed.</param>
    /// <returns><c>true</c> if the handle resolved to a live session, otherwise <c>false</c>.</returns>
    /// <remarks>
    /// <see cref="SessionRegistry"/> is consulted first, since <see cref="GCHandle"/> APIs are unsafe to call
    /// on a handle value that was not itself produced by <c>ToolchainExports.CreateSession</c>.
    /// </remarks>
    internal static bool TryGetSession(nint handle, out NativeSession? session)
    {
        if (!SessionRegistry.IsActive(handle))
        {
            session = null;
            return false;
        }

        try
        {
            session = GCHandle.FromIntPtr(handle).Target as NativeSession;
            return session is not null;
        }
        catch (Exception)
        {
            session = null;
            return false;
        }
    }

    /// <summary>
    /// Copies a managed string to a null-terminated, UTF-8 encoded unmanaged buffer.
    /// </summary>
    /// <param name="value">The string to copy.</param>
    /// <returns>A pointer to the newly allocated buffer, owned by the caller until released via <c>topsyturvy_tc_free</c>.</returns>
    internal static byte* AllocateUtf8String(string value)
    {
        int byteCount = Encoding.UTF8.GetByteCount(value);
        nint buffer = Marshal.AllocHGlobal(byteCount + 1);
        Span<byte> destination = new((void*)buffer, byteCount + 1);
        Encoding.UTF8.GetBytes(value, destination);
        destination[byteCount] = 0;
        return (byte*)buffer;
    }
}
