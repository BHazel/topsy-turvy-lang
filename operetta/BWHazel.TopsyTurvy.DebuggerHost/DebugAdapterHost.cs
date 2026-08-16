using System;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Debugger;
using Microsoft.Extensions.DependencyInjection;
using OmniSharp.Extensions.DebugAdapter.Protocol;
using OmniSharp.Extensions.JsonRpc;

namespace BWHazel.TopsyTurvy.DebuggerHost;

/// <summary>
/// Builds and runs the Debug Adapter Protocol server over stdio.
/// </summary>
public static class DebugAdapterHost
{
    /// <summary>
    /// Starts the DAP server over standard input/output and runs until the client disconnects.
    /// </summary>
    /// <param name="cancellationToken">A token that ends the host if cancelled.</param>
    /// <returns>A task that completes once the client has disconnected.</returns>
    public static async Task RunAsync(CancellationToken cancellationToken)
    {
        DebugAdapterExitSignal exitSignal = new();

        using JsonRpcServer server = await JsonRpcServer.From(options =>
            options
                .WithInput(Console.OpenStandardInput())
                .WithOutput(Console.OpenStandardOutput())
                .WithSerializer(new DapSerializer())
                .WithReceiver(new DapReceiver())
                .WithServices(services => services
                    .AddSingleton<DebugSession>()
                    .AddSingleton(exitSignal)
                    .AddSingleton(new HostCancellationToken(cancellationToken)))
                .AddHandler(typeof(InitializeHandler), new JsonRpcHandlerOptions())
                .AddHandler(typeof(LaunchHandler), new JsonRpcHandlerOptions())
                .AddHandler(typeof(SetBreakpointsHandler), new JsonRpcHandlerOptions())
                .AddHandler(typeof(ConfigurationDoneHandler), new JsonRpcHandlerOptions())
                .AddHandler(typeof(ContinueHandler), new JsonRpcHandlerOptions())
                .AddHandler(typeof(NextHandler), new JsonRpcHandlerOptions())
                .AddHandler(typeof(StepInHandler), new JsonRpcHandlerOptions())
                .AddHandler(typeof(StepOutHandler), new JsonRpcHandlerOptions())
                .AddHandler(typeof(PauseHandler), new JsonRpcHandlerOptions())
                .AddHandler(typeof(StackTraceHandler), new JsonRpcHandlerOptions())
                .AddHandler(typeof(ScopesHandler), new JsonRpcHandlerOptions())
                .AddHandler(typeof(VariablesHandler), new JsonRpcHandlerOptions())
                .AddHandler(typeof(EvaluateHandler), new JsonRpcHandlerOptions())
                .AddHandler(typeof(DisconnectHandler), new JsonRpcHandlerOptions())
                // No typed handler exists for the DAP <c>threads</c> request in <c>OmniSharp.Extensions.DebugAdapter</c> 0.19.9,
                // so it is registered directly here via the generic <c>OnRequest</c> method rather than through a dedicated
                // handler class.
                .OnRequest(
                    "threads",
                    (object? _, CancellationToken _) => Task.FromResult(new ThreadsResult(
                        [new ThreadInfo(DebugSessionConstants.SingleThreadId, "main")])),
                    new JsonRpcHandlerOptions()),
            cancellationToken);

        DebugSession session = (DebugSession)((IServiceProvider)server).GetService(typeof(DebugSession))!;

        // OmniSharp.Extensions.DebugAdapter 0.19.9 defines IOnDebugAdapterServerInitialized but never actually
        // invokes an implementation of it, unlike its LSP counterpart, so DebugEventBridge is wired up explicitly
        // here instead of relying on that lifecycle hook.
        new DebugEventBridge(session, server).Subscribe();

        await exitSignal.Exited;
    }
}
