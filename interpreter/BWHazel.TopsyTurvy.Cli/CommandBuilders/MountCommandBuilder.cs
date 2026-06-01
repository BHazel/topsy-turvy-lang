using System;
using System.CommandLine;
using System.IO;

namespace BWHazel.TopsyTurvy.Cli.CommandBuilders;

/// <summary>
/// Builder for the <c>mount</c> command.
/// </summary>
public static class MountCommandBuilder
{
    /// <summary>
    /// Builds and configures the <c>mount</c> command.
    /// </summary>
    /// <returns>A configured <see cref="Command"/> instance.</returns>
    public static Command Build()
    {
        Command mountCommand = new("mount", "Creates a new Topsy Turvy project.");
        mountCommand.Aliases.Add("init");

        Argument<string> projectArgument = new("project")
        {
            Description = "The project directory to create."
        };

        Option<string?> titleOption = new("--title")
        {
            Description = $"The title of the programme.  Defaults to \"{FileManager.DefaultProgrammeTitle}\"."
        };

        titleOption.Aliases.Add("-t");

        Option<string?> orOption = new("--or")
        {
            Description = "The optional subtitle of the programme."
        };

        Option<bool> hollowOption = new("--hollow")
        {
            Description = "Creates an empty project directory with no default Topsy Turvy file."
        };

        Option<bool> tiptoeOption = new("--tiptoe")
        {
            Description = "Only prints program output and thus chooses to discard aestheticism."
        };

        mountCommand.Arguments.Add(projectArgument);
        mountCommand.Options.Add(titleOption);
        mountCommand.Options.Add(orOption);
        mountCommand.Options.Add(hollowOption);
        mountCommand.Options.Add(tiptoeOption);

        mountCommand.SetAction(parseResult =>
            HandleMount(
                parseResult.GetValue(projectArgument) ?? string.Empty,
                parseResult.GetValue(titleOption) ?? FileManager.DefaultProgrammeTitle,
                parseResult.GetValue(orOption),
                parseResult.GetValue(hollowOption),
                parseResult.GetValue(tiptoeOption)));

        return mountCommand;
    }

    /// <summary>
    /// Handles the execution of the <c>mount</c> command.
    /// </summary>
    /// <param name="project">The project directory to create.</param>
    /// <param name="title">The programme title.</param>
    /// <param name="subtitle">The optional programme subtitle.</param>
    /// <param name="hollow">A value indicating whether to create an empty directory with no default file.</param>
    /// <param name="tiptoe">A value indicating whether to only print program output.</param>
    /// <returns>An integer exit code with 0 for success or 1 for failure.</returns>
    private static int HandleMount(string project, string title, string? subtitle, bool hollow, bool tiptoe)
    {
        if (Directory.Exists(project))
        {
            string message = $"Directory already exists: '{project}'.";
            if (tiptoe)
            {
                Console.Error.WriteLine(message);
            }
            else
            {
                PanelHelper.WriteUserError(message);
            }

            return 1;
        }

        try
        {
            Directory.CreateDirectory(project);

            if (!hollow)
            {
                string dirName = Path.GetFileName(project.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                string filename = Path.Combine(project, $"{dirName}.topsy");
                string fileContent = FileManager.BuildFileContent(title, subtitle);
                File.WriteAllText(filename, fileContent, FileManager.Utf8NoBom);
            }
        }
        catch (IOException ex)
        {
            string message = $"Could not create project '{project}': {ex.Message}";
            if (tiptoe)
            {
                Console.Error.WriteLine(message);
            }
            else
            {
                PanelHelper.WriteRuntimeErrors(message);
            }

            return 1;
        }

        if (!tiptoe)
        {
            PanelHelper.WriteSuccess("Mounted!", $"[cyan]{project}/[/]");
        }

        return 0;
    }
}
