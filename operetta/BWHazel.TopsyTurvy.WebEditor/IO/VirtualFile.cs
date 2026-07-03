using System.Text.Json.Serialization;

namespace BWHazel.TopsyTurvy.WebEditor.IO;

/// <summary>
/// Represents a file in the in-memory virtual file system.
/// </summary>
public sealed class VirtualFile
{
    /// <summary>
    /// Initialises a new instance of the <see cref="VirtualFile"/> class.
    /// </summary>
    /// <param name="name">The filename including the <c>.topsy</c> extension.</param>
    /// <param name="content">The initial source text of the file.</param>
    [JsonConstructor]
    public VirtualFile(string name, string content = "")
    {
        this.Name = name;
        this.Content = content;
    }

    /// <summary>
    /// Gets the filename, including the <c>.topsy</c> extension.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; }

    /// <summary>
    /// Gets or sets the current source text of the file.
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; }
}
