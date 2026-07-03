using System.CommandLine;
using BWHazel.TopsyTurvy.Cli.Repl;

namespace BWHazel.TopsyTurvy.Cli.CommandBuilders;

/// <summary>
/// Builder for the <c>cadenza</c> command.
/// </summary>
public static class CadenzaCommandBuilder
{
    /// <summary>
    /// Builds and configures the <c>cadenza</c> command.
    /// </summary>
    /// <returns>A configured <see cref="Command"/> instance.</returns>
    public static Command Build()
    {
        Command cadenzaCommand = new("cadenza", "Starts the interactive REPL for the Topsy Turvy language.");
        cadenzaCommand.Aliases.Add("interactive");

        Option<bool> tiptoeOption = new("--tiptoe")
        {
            Description = "A REPL with no styling that chooses to discard aestheticism."
        };

        cadenzaCommand.Options.Add(tiptoeOption);

        cadenzaCommand.SetAction(parseResult =>
        {
            ReplSession replSession = new();
            replSession.Run(parseResult.GetValue(tiptoeOption));
            return 0;
        });

        return cadenzaCommand;
    }
}
