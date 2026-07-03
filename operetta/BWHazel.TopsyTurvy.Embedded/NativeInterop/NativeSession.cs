using System.Threading;
using BWHazel.TopsyTurvy.Runtime;

namespace BWHazel.TopsyTurvy.Embedded.NativeInterop;

/// <summary>
/// Holds the per-session state for a native session.
/// </summary>
/// <remarks>
/// A session is created by <see cref="NativeExports.CreateSession"/> and destroyed by
/// <see cref="NativeExports.DestroySession"/>. It is passed across the C ABI boundary as an opaque
/// <see cref="System.Runtime.InteropServices.GCHandle"/>-backed pointer. No managed layout of this
/// type is ever visible externally.
/// </remarks>
/// <param name="callbacks">The output and import-resolution callbacks registered by the caller.</param>
public sealed unsafe class NativeSession(NativeCallbacks callbacks)
{
    /// <summary>
    /// Gets the output and import-resolution callbacks registered at session creation.
    /// </summary>
    public NativeCallbacks Callbacks { get; } = callbacks;

    /// <summary>
    /// Gets the persistent execution environment reused across calls to <see cref="NativeExports.ExecuteProgramme"/>.
    /// </summary>
    public TopsyTurvyEnvironment Environment { get; } = TopsyTurvyEnvironment.CreateGlobal();

    /// <summary>
    /// Gets or sets the cancellation source for the currently running, or most recently run, execution.
    /// </summary>
    /// <remarks>
    /// A fresh instance is created at the start of each <see cref="NativeExports.ExecuteProgramme"/> call, so
    /// <see cref="NativeExports.CancelExecution"/> called before any execution has started is a no-op.
    /// </remarks>
    public CancellationTokenSource? CancellationTokenSource { get; set; }

    /// <summary>
    /// Gets or sets the message of the most recently caught exception in any export using this session.
    /// </summary>
    /// <remarks>
    /// Retrieved via <see cref="NativeExports.GetLastError"/>; <c>null</c> when no error has occurred.
    /// </remarks>
    public string? LastError { get; set; }
}
