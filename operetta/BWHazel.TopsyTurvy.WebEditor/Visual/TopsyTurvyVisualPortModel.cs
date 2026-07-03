using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;

namespace BWHazel.TopsyTurvy.WebEditor.Visual;

/// <summary>
/// Extends <see cref="PortModel"/> with a display label and role for the visual editor.
/// </summary>
public sealed class TopsyTurvyVisualPortModel : PortModel
{
    /// <summary>
    /// Initialises a new <see cref="TopsyTurvyVisualPortModel"/>.
    /// </summary>
    /// <param name="parent">The node that owns this port.</param>
    /// <param name="alignment">The edge of the node where the port is drawn.</param>
    /// <param name="label">The human-readable label shown beside the port circle.</param>
    /// <param name="role">The port role, either execution flow or data.</param>
    public TopsyTurvyVisualPortModel(
        TopsyTurvyVisualNodeModel parent,
        PortAlignment alignment,
        string label,
        VisualPortRole role)
        : base(parent, alignment, Point.Zero, Size.Zero)
    {
        this.Label = label;
        this.Role = role;
    }

    /// <summary>
    /// Gets or sets the human-readable label shown beside this port.
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// Gets or sets the role of this port.
    /// </summary>
    /// <remarks>
    /// This determines if the port is used for execution flow or data.
    /// </remarks>
    public VisualPortRole Role { get; set; }
}
