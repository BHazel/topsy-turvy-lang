using System.CommandLine;
using System.Threading;
using System.Threading.Tasks;
using BWHazel.TopsyTurvy.Cli.Debug;
using BWHazel.TopsyTurvy.Debugger;
using BWHazel.TopsyTurvy.DebuggerHost;

namespace BWHazel.TopsyTurvy.Cli.CommandBuilders;

/// <summary>
/// Builder for the <c>director</c> command.
/// </summary>
public static class DirectorCommandBuilder
{
    /// <summary>
    /// Builds and configures the <c>director</c> command.
    /// </summary>
    /// <remarks>
    /// The --adapter flag is hidden from help output, since it is launched automatically by the VS Code extension
    /// rather than typed by hand.  When present, it routes execution to <see cref="HandleAdapterAsync"/>, starting the
    /// DAP server over stdio instead of the interactive debug console.
    /// </remarks>
    /// <returns>A configured <see cref="Command"/> instance.</returns>
    public static Command Build()
    {
        Command directorCommand = new("director", "Starts an interactive debug session against a Topsy Turvy .topsy file.");
        directorCommand.Aliases.Add("debug");

        Argument<string> fileArgument = new("filename")
        {
            Description = "The Topsy Turvy file to debug.",
            Arity = ArgumentArity.ZeroOrOne
        };

        Option<bool> tiptoeOption = new("--tiptoe")
        {
            Description = "Only prints program output and thus chooses to discard aestheticism."
        };

        Option<bool> adapterOption = new("--adapter")
        {
            Description = "Starts the Debug Adapter Protocol server over stdio for VS Code, rather than the interactive console.",
            Hidden = true
        };

        directorCommand.Arguments.Add(fileArgument);
        directorCommand.Options.Add(tiptoeOption);
        directorCommand.Options.Add(adapterOption);

        directorCommand.SetAction((parseResult, cancellationToken) =>
            parseResult.GetValue(adapterOption)
                ? HandleAdapterAsync(cancellationToken)
                : HandleDirectorAsync(
                    parseResult.GetValue(fileArgument) ?? string.Empty,
                    parseResult.GetValue(tiptoeOption),
                    cancellationToken));

        return directorCommand;
    }

    /// <summary>
    /// Handles execution of the <c>director</c> command by running the interactive debug console.
    /// </summary>
    /// <param name="filename">The filename of the Topsy Turvy file to debug.</param>
    /// <param name="tiptoe">A value indicating whether to only print program output.</param>
    /// <param name="cancellationToken">A token that ends the session if cancelled.</param>
    /// <returns>An integer exit code with 0 for success or 1 for failure.</returns>
    private static async Task<int> HandleDirectorAsync(string filename, bool tiptoe, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filename))
        {
            return PanelHelper.ReportUserError(tiptoe, "No file specified.");
        }

        DebugSession session = new();
        DebugConsole console = new(session, tiptoe);
        return await console.RunAsync(filename, cancellationToken);
    }

    /// <summary>
    /// Handles execution of the <c>director --adapter</c> command by running the DAP server over stdio.
    /// </summary>
    /// <param name="cancellationToken">A token that ends the host if cancelled.</param>
    /// <returns>An integer exit code, always 0 for a normal client disconnect.</returns>
    private static async Task<int> HandleAdapterAsync(CancellationToken cancellationToken)
    {
        await DebugAdapterHost.RunAsync(cancellationToken);
        return 0;
    }
}
