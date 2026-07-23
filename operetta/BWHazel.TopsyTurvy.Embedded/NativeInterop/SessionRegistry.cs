using System.Collections.Generic;
using System.Threading;

namespace BWHazel.TopsyTurvy.Embedded.NativeInterop;

/// <summary>
/// Tracks which session handles are currently valid so session-resolving exports can safely reject
/// invalid or already-destroyed handles.
/// </summary>
/// <remarks>
/// Calling <see cref="System.Runtime.InteropServices.GCHandle.FromIntPtr"/>, <c>.Free()</c>, or <c>.Target</c>
/// on a handle value that was never allocated by <see cref="NativeExports.ToolchainExports.CreateSession"/> (or was already
/// freed) is undefined behaviour at the runtime level: it is not guaranteed to surface as a catchable managed
/// exception and can crash the host process. This registry is consulted first so those APIs are
/// only ever invoked on a value this class itself has vouched for.
/// </remarks>
public static class SessionRegistry
{
    private static readonly HashSet<nint> activeHandles = [];
    private static readonly Lock gate = new();

    /// <summary>
    /// Registers a handle as valid, immediately after allocation.
    /// </summary>
    /// <param name="handle">The handle to register.</param>
    public static void Register(nint handle)
    {
        lock (gate)
        {
            activeHandles.Add(handle);
        }
    }

    /// <summary>
    /// Removes a handle from the registry, if present.
    /// </summary>
    /// <param name="handle">The handle to unregister.</param>
    /// <returns><c>true</c> if the handle was registered and has now been removed, otherwise <c>false</c>.</returns>
    public static bool TryUnregister(nint handle)
    {
        lock (gate)
        {
            return activeHandles.Remove(handle);
        }
    }

    /// <summary>
    /// Determines whether a handle is currently registered as valid.
    /// </summary>
    /// <param name="handle">The handle to check.</param>
    /// <returns><c>true</c> if the handle is registered, otherwise <c>false</c>.</returns>
    public static bool IsActive(nint handle)
    {
        lock (gate)
        {
            return activeHandles.Contains(handle);
        }
    }
}
