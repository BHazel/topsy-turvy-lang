using System.Threading;

namespace BWHazel.TopsyTurvy.DebuggerHost;

/// <summary>
/// The cancellation token for the lifetime of the whole DAP host, distinct from the per-request token a handler's
/// own <c>Handle</c> method receives.
/// </summary>
/// <remarks>
/// <see cref="CancellationToken"/> is a struct, so it cannot be registered directly as a DI service; this record
/// wraps it in a reference type so it can be injected the same way as any other singleton, for example into
/// <see cref="LaunchHandler"/>, which needs a token that outlives its own per-request one.
/// </remarks>
/// <param name="Token">The token for the lifetime of the host.</param>
public sealed record HostCancellationToken(CancellationToken Token);
