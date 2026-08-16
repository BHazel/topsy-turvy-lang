using System;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Debugger;
using OmniSharp.Extensions.DebugAdapter.Protocol.Requests;

namespace BWHazel.TopsyTurvy.DebuggerHost;

/// <summary>
/// Evaluates a watch expression or Debug Console command against a paused session.
/// </summary>
/// <param name="session">The session this host manages.</param>
/// <remarks>
/// <para>
/// This handler handles the following DAP request:
/// * <c>evaluate</c>: Evaluates the given expression in the context of a stack frame.
/// </para>
/// <para>
/// Used for both the Debug Console and hover-evaluate. A failure, such as a mistyped expression or evaluating
/// while the session is not paused, is a routine outcome, not a host fault: a failed <see cref="EvaluationResult"/>
/// is returned as a normal response with the failure message as the result text, never as an unhandled exception
/// or a DAP protocol-level error.
/// </para>
/// <para>
/// A missing frame ID, sent by the Debug Console when nothing is selected in the Call Stack panel, falls back to
/// the top frame, rather than the literal <c>0</c> the request would otherwise carry, which is never a valid frame
/// ID (frame IDs start at 1).
/// </para>
/// </remarks>
public sealed class EvaluateHandler(DebugSession session) : EvaluateHandlerBase
{
    /// <inheritdoc/>
    public override Task<EvaluateResponse> Handle(EvaluateArguments request, CancellationToken cancellationToken)
    {
        try
        {
            int frameId = (int)(request.FrameId ?? session.GetCallStack()[0].Id);
            EvaluationResult result = session.Evaluate(request.Expression, frameId);

            return Task.FromResult(new EvaluateResponse()
            {
                Result = result.IsSuccess
                    ? result.Value!.ToString() ?? string.Empty
                    : $"Error: {result.ErrorMessage}",
                VariablesReference = 0
            });
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return Task.FromResult(new EvaluateResponse()
            {
                Result = $"Error: {ex.Message}", VariablesReference = 0
            });
        }
    }
}
