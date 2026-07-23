using BWHazel.TopsyTurvy.Embedded.NativeExports;

namespace BWHazel.TopsyTurvy.Tests.Embedded.NativeExports;

/// <summary>
/// Shared session lifecycle helpers for every native export test class in this namespace, whichever
/// library namespace it belongs to.
/// </summary>
/// <remarks>
/// Every export is <see cref="System.Runtime.InteropServices.UnmanagedCallersOnlyAttribute"/>-annotated, so
/// the C# compiler forbids calling them directly even from in-process test code.  A function pointer must
/// be obtained via <c>&amp;ToolchainExports.Method</c> and invoked through that, exactly as a native caller
/// would resolve the exported symbol.
/// </remarks>
internal static unsafe class NativeExportsTestSupport
{
    /// <summary>
    /// Creates a session registered with <see cref="TestCallbackCapture"/> callback targets.
    /// </summary>
    /// <returns>The created session handle.</returns>
    internal static nint CreateSession()
    {
        delegate* unmanaged<
            delegate* unmanaged<nint, byte*, byte, void>,
            delegate* unmanaged<nint, byte*, byte*>,
            delegate* unmanaged<nint, byte*>,
            nint,
            nint> createSession = &ToolchainExports.CreateSession;
        return createSession(&TestCallbackCapture.OnOutputLine, &TestCallbackCapture.OnResolveImport, &TestCallbackCapture.OnInputLine, 0);
    }

    /// <summary>
    /// Destroys a session via <see cref="ToolchainExports.DestroySession"/>.
    /// </summary>
    /// <param name="session">The session handle to destroy.</param>
    internal static void DestroySession(nint session)
    {
        delegate* unmanaged<nint, void> destroySession = &ToolchainExports.DestroySession;
        destroySession(session);
    }

    /// <summary>
    /// Releases a buffer via <see cref="ToolchainExports.FreeBuffer"/>.
    /// </summary>
    /// <param name="pointer">The buffer to release.</param>
    internal static void FreeBuffer(byte* pointer)
    {
        delegate* unmanaged<byte*, void> freeBuffer = &ToolchainExports.FreeBuffer;
        freeBuffer(pointer);
    }
}
