using System;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.DebugAdapter.Protocol.Events;
using OmniSharp.Extensions.DebugAdapter.Protocol.Requests;
using OmniSharp.Extensions.JsonRpc;

namespace BWHazel.TopsyTurvy.DebuggerHost;

/// <summary>
/// Reports this adapter's capabilities to the client.
/// </summary>
/// <param name="responseRouter">The route used to send the separate <c>initialized</c> event.</param>
/// <remarks>
/// <para>
/// This handler handles the following DAP request:
/// * <c>initialize</c>: The client requests the capabilities of this debug adapter.
/// </para>
/// <para>
/// Advertises no capability beyond <c>supportsConfigurationDoneRequest</c>: only unconditional line breakpoints
/// and launch requests are supported, so no conditional-breakpoint or attach-related capability is advertised.
/// </para>
/// <para>
/// The response only reports capabilities; a compliant client waits for a separate <c>initialized</c> event
/// before sending <c>setBreakpoints</c> or <c>configurationDone</c>. That event is sent after a short deferred
/// delay, so it reliably reaches the client after this response rather than racing it on the same output queue.
/// </para>
/// </remarks>
public sealed class InitializeHandler(IResponseRouter responseRouter) : DebugAdapterInitializeHandlerBase
{
    /// <inheritdoc/>
    public override Task<InitializeResponse> Handle(InitializeRequestArguments request, CancellationToken cancellationToken)
    {
        _ = Task.Delay(TimeSpan.FromMilliseconds(50)).ContinueWith(
            _ => responseRouter.SendNotification(new InitializedEvent()),
            TaskScheduler.Default);

        return Task.FromResult(new InitializeResponse()
        {
            SupportsConfigurationDoneRequest = true
        });
    }
}
