using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Debugger;
using OmniSharp.Extensions.DebugAdapter.Protocol.Requests;

namespace BWHazel.TopsyTurvy.DebuggerHost;

/// <summary>
/// Starts the target programme running once initial breakpoints have been sent.
/// </summary>
/// <param name="session">The session this host manages.</param>
/// <remarks>
/// <para>
/// This handler handles the following DAP request:
/// * <c>configurationDone</c>: The client has finished sending initial configuration (breakpoints), signalling
/// that debugging can begin.
/// </para>
/// <para>
/// This is where the target programme actually starts running for the first time. <see cref="LaunchHandler"/>
/// only parses and pauses at the first statement, deliberately not starting execution itself, so that a
/// breakpoint set between <c>launch</c> and <c>configurationDone</c> is guaranteed to already be in place once
/// the programme does start. Unlike the CLI debugger, a DAP client has no separate gesture for this first resume,
/// so it has to happen here.
/// </para>
/// </remarks>
public sealed class ConfigurationDoneHandler(DebugSession session) : ConfigurationDoneHandlerBase
{
    /// <inheritdoc/>
    public override Task<ConfigurationDoneResponse> Handle(ConfigurationDoneArguments request, CancellationToken cancellationToken)
    {
        session.Continue();
        return Task.FromResult(new ConfigurationDoneResponse());
    }
}
