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

        Option<string[]> admitOption = new("--admit")
        {
            Description = "Loads an external .NET assembly (.dll) so its functions become callable via SUMMON.  Can be specified multiple times."
        };

        admitOption.Aliases.Add("--include");

        cadenzaCommand.Options.Add(tiptoeOption);
        cadenzaCommand.Options.Add(admitOption);

        cadenzaCommand.SetAction(parseResult =>
        {
            ReplSession replSession = new();
            return replSession.Run(parseResult.GetValue(tiptoeOption), parseResult.GetValue(admitOption) ?? []);
        });

        return cadenzaCommand;
    }
}
