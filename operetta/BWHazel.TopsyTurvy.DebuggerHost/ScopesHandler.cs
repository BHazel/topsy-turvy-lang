using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Debugger;
using OmniSharp.Extensions.DebugAdapter.Protocol.Models;
using OmniSharp.Extensions.DebugAdapter.Protocol.Requests;

namespace BWHazel.TopsyTurvy.DebuggerHost;

/// <summary>
/// Returns the labelled variable scopes visible from a given stack frame.
/// </summary>
/// <param name="session">The session this host manages.</param>
/// <remarks>
/// <para>
/// This handler handles the following DAP request:
/// * <c>scopes</c>: The client requests the variable scopes for a given stack frame.
/// </para>
/// <para>
/// The <see cref="Scope.VariablesReference"/> of each returned <see cref="Scope"/> is encoded via
/// <see cref="VariablesReferenceCodec"/> so <see cref="VariablesHandler"/> can decode it back into the originating
/// frame ID and scope index without any separate reference-tracking state.
/// </para>
/// </remarks>
public sealed class ScopesHandler(DebugSession session) : ScopesHandlerBase
{
    /// <inheritdoc/>
    public override Task<ScopesResponse> Handle(ScopesArguments request, CancellationToken cancellationToken)
    {
        int frameId = (int)request.FrameId;
        IReadOnlyList<VariableScope> scopes = session.GetVariables(frameId);

        List<Scope> dapScopes = [.. scopes.Select((scope, index) => new Scope()
        {
            Name = scope.Label,
            VariablesReference = VariablesReferenceCodec.Encode(frameId, index),
            Expensive = false
        })];

        return Task.FromResult(new ScopesResponse()
        {
            Scopes = new Container<Scope>(dapScopes)
        });
    }
}
