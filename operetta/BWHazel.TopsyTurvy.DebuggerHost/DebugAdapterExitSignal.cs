using System.Threading.Tasks;

namespace BWHazel.TopsyTurvy.DebuggerHost;

/// <summary>
/// Signals that the DAP host process should exit, rather than running indefinitely.
/// </summary>
/// <remarks>
/// Registered as a singleton so that <see cref="DisconnectHandler"/>, constructed per request, and
/// <see cref="DebugAdapterHost.RunAsync"/>, the long-running method that starts the server, share the same
/// instance via dependency injection with no direct reference between them: the handler completes it once the
/// client disconnects, and the host awaits <see cref="Exited"/> to know when to stop.
/// </remarks>
public sealed class DebugAdapterExitSignal
{
    private readonly TaskCompletionSource completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Gets a task that completes once <see cref="Complete"/> has been called.
    /// </summary>
    public Task Exited => this.completionSource.Task;

    /// <summary>
    /// Signals that the host should exit.
    /// </summary>
    public void Complete() => this.completionSource.TrySetResult();
}
