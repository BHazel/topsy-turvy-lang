using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Debugger;
using OmniSharp.Extensions.DebugAdapter.Protocol.Models;
using OmniSharp.Extensions.DebugAdapter.Protocol.Requests;

namespace BWHazel.TopsyTurvy.DebuggerHost;

/// <summary>
/// Returns the variables belonging to a given scope.
/// </summary>
/// <param name="session">The session this host manages.</param>
/// <remarks>
/// This handler handles the following DAP request:
/// * <c>variables</c>: Retrieves all child variables for the given variable reference.
/// </remarks>
public sealed class VariablesHandler(DebugSession session) : VariablesHandlerBase
{
    /// <inheritdoc/>
    public override Task<VariablesResponse> Handle(VariablesArguments request, CancellationToken cancellationToken)
    {
        (int frameId, int scopeIndex) = VariablesReferenceCodec.Decode(request.VariablesReference);
        IReadOnlyList<VariableScope> scopes = session.GetVariables(frameId);
        VariableScope scope = scopes[scopeIndex];

        List<Variable> variables = [.. scope.Variables.Select(pair => new Variable()
        {
            Name = pair.Key,
            Value = pair.Value.ToString() ?? string.Empty,
            VariablesReference = 0
        })];

        return Task.FromResult(new VariablesResponse()
        {
            Variables = new Container<Variable>(variables)
        });
    }
}
