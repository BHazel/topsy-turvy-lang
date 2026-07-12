using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace BWHazel.TopsyTurvy.WebEditor.IO;

/// <summary>
/// Represents the editor preferences stored in Local Storage.
/// </summary>
public sealed class EditorPreferences
{
    /// <summary>
    /// Gets or initialises the open files and their content.
    /// </summary>
    [JsonPropertyName("files")]
    public List<VirtualFile>? Files { get; init; }

    /// <summary>
    /// Gets or initialises the name of the active file.
    /// </summary>
    [JsonPropertyName("activeFile")]
    public string? ActiveFile { get; init; }

    /// <summary>
    /// Gets or initialises the command-line arguments text.
    /// </summary>
    [JsonPropertyName("commandLineArguments")]
    public string? CommandLineArguments { get; init; }

    /// <summary>
    /// Gets or initialises the standard input text.
    /// </summary>
    [JsonPropertyName("standardInput")]
    public string? StandardInput { get; init; }

    /// <summary>
    /// Gets or initialises a value indicating whether G&amp;S labels are enabled.
    /// </summary>
    [JsonPropertyName("areGsLabelsEnabled")]
    public bool? AreGsLabelsEnabled { get; init; }

    /// <summary>
    /// Gets or initialises a value indicating whether dark mode is active.
    /// </summary>
    [JsonPropertyName("isDarkMode")]
    public bool? IsDarkMode { get; init; }

    /// <summary>
    /// Gets or initialises the terminal output.
    /// </summary>
    [JsonPropertyName("terminalOutput")]
    public string? TerminalOutput { get; init; }
}
