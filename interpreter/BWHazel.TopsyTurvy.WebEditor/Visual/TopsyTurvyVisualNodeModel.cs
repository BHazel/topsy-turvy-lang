using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using BWHazel.TopsyTurvy.Ast;

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

    /// <summary>
    /// Gets or sets the concrete AST type name this node represents.
    /// </summary>
    /// <remarks>
    /// Read by <c>VisualGraphToAstConverter</c> to reconstruct the correct AST node without reference equality.
    /// </remarks>
    public string? StatementType { get; set; }

    /// <summary>
    /// Gets or sets the original AST node from which this visual node was built.
    /// </summary>
    /// <remarks>
    /// Used by <c>VisualGraphToAstConverter</c> as a fallback for structural reconstruction.
    /// </remarks>
    public object? AstNode { get; set; }

    /// <summary>
    /// Gets or sets the variable or function name associated with this node.
    /// </summary>
    /// <remarks>
    /// Populated for Declaration, Assignment target, Identifier and Function opener nodes.
    /// </remarks>
    public string? SymbolIdentifierNodeName { get; set; }

    /// <summary>
    /// Gets or sets the Topsy Turvy type associated with this node.
    /// </summary>
    /// <remarks>
    /// Populated for Declaration and Literal nodes.
    /// </remarks>
    public LiteralType? NodeLiteralType { get; set; }

    /// <summary>
    /// Gets or sets the element type for array declaration nodes.
    /// </summary>
    public LiteralType? ArrayElementLiteralType { get; set; }

    /// <summary>
    /// Gets or sets the literal value as a string, used for editable Literal nodes.
    /// </summary>
    public string? LiteralValue { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this node represents a constant declaration.
    /// </summary>
    public bool IsIdentifierConstant { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether a print statement suppresses the trailing newline.
    /// </summary>
    public bool PrintSuppressNewline { get; set; }

    /// <summary>
    /// Gets or sets the node ID of the paired closer node for block-opener nodes.
    /// </summary>
    /// <remarks>
    /// Set by <c>VisualGraphBuilder</c> and used by the "Delete block" context-menu action.
    /// </remarks>
    public string? PairedCloserId { get; set; }

    /// <summary>
    /// Gets or sets the node ID of the paired opener node for block-closer nodes.
    /// </summary>
    public string? PairedOpenerId { get; set; }
}
