using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Debugger;
using OmniSharp.Extensions.DebugAdapter.Protocol.Events;
using OmniSharp.Extensions.JsonRpc;

namespace BWHazel.TopsyTurvy.DebuggerHost;

/// <summary>
/// Translates the <see cref="DebugSession.Paused"/>, <see cref="DebugSession.Output"/>, and
/// <see cref="DebugSession.Ended"/> events of a <see cref="DebugSession"/> into DAP <c>stopped</c>, <c>output</c>,
/// <c>exited</c> and <c>terminated</c> events sent to the client.
/// </summary>
/// <param name="session">The session to observe.</param>
/// <param name="responseRouter">The route used to send each translated event.</param>
/// <remarks>
/// Constructed and subscribed explicitly from <see cref="DebugAdapterHost.RunAsync"/> rather than an automatic
/// lifecycle hook, since sending a DAP event only needs <see cref="IResponseRouter.SendNotification(MediatR.IRequest)"/>
/// underneath, which <c>JsonRpcServer</c> already implements directly.
/// </remarks>
internal sealed class DebugEventBridge(DebugSession session, IResponseRouter responseRouter)
{
    /// <summary>
    /// Subscribes to the events of the session for the lifetime of the session.
    /// </summary>
    internal void Subscribe()
    {
        session.Paused += (_, eventArgs) => responseRouter.SendNotification(new StoppedEvent()
        {
            Reason = ToStoppedEventReason(eventArgs.Reason),
            ThreadId = DebugSessionConstants.SingleThreadId,
            AllThreadsStopped = true
        });

        session.Output += (_, eventArgs) => responseRouter.SendNotification(new OutputEvent()
        {
            Category = OutputEventCategory.StandardOutput,
            Output = eventArgs.Text + "\n"
        });

        session.Ended += (_, eventArgs) =>
        {
            foreach (Diagnostic diagnostic in eventArgs.Diagnostics.Diagnostics)
            {
                responseRouter.SendNotification(new OutputEvent()
                {
                    Category = OutputEventCategory.StandardError,
                    Output = $"[{diagnostic.Span.Start.Line}:{diagnostic.Span.Start.Column}] {diagnostic.Message}\n"
                });
            }

            responseRouter.SendNotification(new ExitedEvent()
            {
                ExitCode = eventArgs.Diagnostics.HasErrors
                    ? 1
                    : eventArgs.ExitCode
            });
            
            responseRouter.SendNotification(new TerminatedEvent());
        };
    }

    /// <summary>
    /// Maps a <see cref="PauseReason"/> to its DAP <see cref="StoppedEventReason"/> equivalent.
    /// </summary>
    /// <param name="reason">The reason to map.</param>
    /// <returns>The equivalent DAP reason.</returns>
    private static StoppedEventReason ToStoppedEventReason(PauseReason reason) => reason switch
    {
        PauseReason.BreakpointHit => StoppedEventReason.Breakpoint,
        PauseReason.StepComplete => StoppedEventReason.Step,
        PauseReason.UnhandledError => StoppedEventReason.Exception,
        PauseReason.ManualPause => StoppedEventReason.Pause,
        _ => StoppedEventReason.Pause
    };
}
