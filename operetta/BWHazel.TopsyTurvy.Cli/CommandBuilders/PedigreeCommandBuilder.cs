using System;
using System.CommandLine;
using System.Reflection;

namespace BWHazel.TopsyTurvy.Cli.CommandBuilders;

/// <summary>
/// Builder for the <c>pedigree</c> command.
/// </summary>
public static class PedigreeCommandBuilder
{
    private const string TopsyTurvySpecVersion = "0.9.0";
    private const string UtopirSpecVersion = "0.0.1-preview4";

    /// <summary>
    /// Builds and configures the <c>pedigree</c> command.
    /// </summary>
    /// <returns>A configured <see cref="Command"/> instance.</returns>
    public static Command Build()
    {
        Command pedigreeCommand = new("pedigree", "Displays the Topsy Turvy CLI and language spec versions.");
        pedigreeCommand.Aliases.Add("info");

        Option<bool> tiptoeOption = new("--tiptoe")
        {
            Description = "Only prints program output and thus chooses to discard aestheticism."
        };

        pedigreeCommand.Options.Add(tiptoeOption);

        pedigreeCommand.SetAction(parseResult =>
            HandlePedigree(parseResult.GetValue(tiptoeOption)));

        return pedigreeCommand;
    }

    /// <summary>
    /// Handles the execution of the <c>pedigree</c> command.
    /// </summary>
    /// <param name="tiptoe">A value indicating whether to only print program output.</param>
    /// <returns>An integer exit code with 0 for success.</returns>
    private static int HandlePedigree(bool tiptoe)
    {
        string informationalVersion = Assembly
            .GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "unknown";

        string[] versionParts = informationalVersion.Split('+', 2);
        string version = versionParts[0];
        string commitHash = versionParts.Length > 1 ? versionParts[1] : "unknown";

        if (tiptoe)
        {
            Console.WriteLine($"Version: {version}");
            Console.WriteLine($"Commit: {commitHash}");
            Console.WriteLine($"Topsy Turvy Spec: {TopsyTurvySpecVersion}");
            Console.WriteLine($"UtopIR Spec: {UtopirSpecVersion}");
        }
        else
        {
            PanelHelper.WriteVersionInfo(version, commitHash, TopsyTurvySpecVersion, UtopirSpecVersion);
        }

        return 0;
    }
}
