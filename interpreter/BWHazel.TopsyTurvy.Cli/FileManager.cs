using System.Text;

namespace BWHazel.TopsyTurvy.Cli;

/// <summary>
/// Provides utilities for creating and writing Topsy Turvy source files.
/// </summary>
public static class FileManager
{
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
}
