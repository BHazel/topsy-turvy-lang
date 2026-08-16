using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Debugger;
using OmniSharp.Extensions.DebugAdapter.Protocol.Requests;

namespace BWHazel.TopsyTurvy.DebuggerHost;

/// <summary>
/// Starts a target session for the given programme file, then pauses immediately before its first statement.
/// </summary>
/// <param name="session">The session this host manages.</param>
/// <param name="hostCancellationToken">The token for the lifetime of the whole DAP host, injected separately from
/// the per-request token <see cref="Handle"/> receives.</param>
/// <remarks>
/// <para>
/// This handler handles the following DAP request:
/// * <c>launch</c>: The client requests that the debuggee be launched and start executing.
/// </para>
/// <para>
/// <see cref="DebugSession.StartAsync"/> links the given token into the one the interpreter checks for the rest
/// of the run, so it needs <paramref name="hostCancellationToken"/> rather than the per-request
/// <c>cancellationToken</c> <see cref="Handle"/> receives, which is cancelled as soon as this request completes
/// and would otherwise abort the interpreter silently part-way through the run.
/// </para>
/// </remarks>
public sealed class LaunchHandler(DebugSession session, HostCancellationToken hostCancellationToken) : LaunchHandlerBase
{
    /// <inheritdoc/>
    public override async Task<LaunchResponse> Handle(LaunchRequestArguments request, CancellationToken cancellationToken)
    {
        string program = request.ExtensionData.TryGetValue("program", out object? value)
            ? value?.ToString() ?? string.Empty
            : string.Empty;

        await session.StartAsync(program, hostCancellationToken.Token);
        return new LaunchResponse();
    }
}
