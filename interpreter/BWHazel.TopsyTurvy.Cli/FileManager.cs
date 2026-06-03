using System.IO;
using System.IO.Abstractions;
using System.Text;
using Testably.Abstractions;

namespace BWHazel.TopsyTurvy.Cli;

/// <summary>
/// Provides utilities for creating and writing Topsy Turvy source files.
/// </summary>
public static class FileManager
{
    private static readonly IFileSystem DefaultFileSystem = new RealFileSystem();

    /// <summary>
    /// The default title used for commissioned programmes when no title is provided.
    /// </summary>
    internal static readonly string DefaultProgrammeTitle = "Programme";

    /// <summary>
    /// The default UTF-8 encoding without a byte-order mark used for all written source files.
    /// </summary>
    internal static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// Builds the content of a new Topsy Turvy source file.
    /// </summary>
    /// <param name="title">The programme title.</param>
    /// <param name="subtitle">The optional programme subtitle.</param>
    /// <returns>The file content as a string.</returns>
    public static string BuildFileContent(string title, string? subtitle)
    {
        StringBuilder builder = new();
        builder.AppendLine($"HARK! \"{title}\"");

        if (subtitle is not null)
        {
            builder.AppendLine($"  or, \"{subtitle}\"");
        }

        builder.AppendLine();
        builder.AppendLine("BEHOLD \"Hello, World!\"");
        builder.AppendLine();
        builder.Append("FINALE.");

        return builder.ToString();
    }

    /// <summary>
    /// Attempts to read the source text of a Topsy Turvy file.
    /// </summary>
    /// <param name="filePath">The path to the Topsy Turvy file.</param>
    /// <param name="source">The file contents when successful, otherwise <see cref="string.Empty"/>.</param>
    /// <param name="fileSystem">The file system to use or <c>null</c> to use the real file system.</param>
    /// <returns>A tuple indicating success and an optional error message.</returns>
    public static (bool Success, string? ErrorMessage) TryReadSource(string filePath, out string source, IFileSystem? fileSystem = null)
    {
        IFileSystem fs = fileSystem ?? DefaultFileSystem;
        source = string.Empty;

        if (!fs.File.Exists(filePath))
        {
            return (false, $"File not found: {filePath}");
        }

        try
        {
            source = fs.File.ReadAllText(filePath);
            return (true, null);
        }
        catch (IOException ex)
        {
            return (false, $"Could not read '{filePath}': {ex.Message}");
        }
    }

    /// <summary>
    /// Attempts to create a new Topsy Turvy programme file.
    /// </summary>
    /// <param name="filename">The path to the file to create.</param>
    /// <param name="title">The programme title.</param>
    /// <param name="subtitle">The optional programme subtitle.</param>
    /// <param name="fileSystem">The file system to use, or <c>null</c> to use the real file system.</param>
    /// <returns>A tuple indicating success and an optional error message.</returns>
    public static (bool Success, string? ErrorMessage) TryCreateProgrammeFile(string filename, string title, string? subtitle, IFileSystem? fileSystem = null)
    {
        IFileSystem fs = fileSystem ?? DefaultFileSystem;

        if (fs.File.Exists(filename))
        {
            return (false, $"File already exists: '{filename}'.");
        }

        string fileContent = BuildFileContent(title, subtitle);
        try
        {
            fs.File.WriteAllText(filename, fileContent, Utf8NoBom);
            return (true, null);
        }
        catch (IOException ex)
        {
            return (false, $"Could not create file '{filename}': {ex.Message}");
        }
    }

    /// <summary>
    /// Attempts to create a new Topsy Turvy project directory, optionally with a Topsy Turvy entry-point file.
    /// </summary>
    /// <param name="project">The path to the project directory to create.</param>
    /// <param name="title">The programme title for the scaffold file.</param>
    /// <param name="subtitle">The optional programme subtitle.</param>
    /// <param name="hollow">A value indicating whether to create an empty directory without a Topsy Turvy entry-point file.</param>
    /// <param name="fileSystem">The file system to use or <c>null</c> to use the real file system.</param>
    /// <returns>A tuple indicating success and an optional error message.</returns>
    public static (bool Success, string? ErrorMessage) TryMountProject(string project, string title, string? subtitle, bool hollow, IFileSystem? fileSystem = null)
    {
        IFileSystem fs = fileSystem ?? DefaultFileSystem;

        if (fs.Directory.Exists(project))
        {
            return (false, $"Directory already exists: '{project}'.");
        }

        try
        {
            fs.Directory.CreateDirectory(project);
            if (!hollow)
            {
                string directoryName = fs.Path.GetFileName(project.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                string filename = fs.Path.Combine(project, $"{directoryName}.topsy");
                string fileContent = BuildFileContent(title, subtitle);
                fs.File.WriteAllText(filename, fileContent, Utf8NoBom);
            }

            return (true, null);
        }
        catch (IOException ex)
        {
            return (false, $"Could not create project '{project}': {ex.Message}");
        }
    }
}
