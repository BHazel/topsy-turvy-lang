using System.CommandLine;

namespace BWHazel.TopsyTurvy.Cli.CommandBuilders;

/// <summary>
/// Builder for the <c>sorcerer</c> command.
/// </summary>
public static class SorcererCommandBuilder
{
    /// <summary>
    /// Builds and configures the <c>sorcerer</c> command.
    /// </summary>
    /// <returns>A configured <see cref="Command"/> instance.</returns>
    public static Command Build()
    {
        Command sorcererCommand = new("sorcerer", "Developer tools for the Topsy Turvy language.");
        sorcererCommand.Aliases.Add("dev");

        sorcererCommand.Subcommands.Add(PromptbookCommandBuilder.Build());
        sorcererCommand.Subcommands.Add(IncantationCommandBuilder.Build());

        return sorcererCommand;
    }
}
