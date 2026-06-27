using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;

namespace BWHazel.TopsyTurvy.WebEditor.Visual;

/// <summary>
/// Extends <see cref="NodeModel"/> with Topsy Turvy-specific metadata for display in the visual editor.
/// </summary>
public sealed class TopsyTurvyVisualNodeModel : NodeModel
{
    /// <summary>
    /// Initialises a new instance of the <see cref="TopsyTurvyVisualNodeModel"/> class.
    /// </summary>
    /// <param name="id">The unique identifier used by Z.Blazor.Diagrams for link tracking.</param>
    /// <param name="position">The initial canvas position.</param>
    /// <param name="title">The primary label shown in the node header, typically the Topsy Turvy keyword.</param>
    /// <param name="subtitle">The optional secondary label shown below the header, e.g. variable name and type.</param>
    /// <param name="kind">The colour category that determines the header background via CSS.</param>
    public TopsyTurvyVisualNodeModel(
        string id,
        Point position,
        string title,
        string? subtitle,
        VisualNodeKind kind)
        : base(id, position)
    {
        this.Title = title;
        this.Subtitle = subtitle;
        this.Kind = kind;
    }

    /// <summary>
    /// Gets or sets the optional secondary label shown below the node header.
    /// </summary>
    public string? Subtitle { get; set; }

    /// <summary>
    /// Gets or sets the semantic colour category for the node header.
    /// </summary>
    public VisualNodeKind Kind { get; set; }
}
