namespace BWHazel.TopsyTurvy.WebEditor.Visual;

/// <summary>
/// Defines the role of a port on a visual node.
/// </summary>
public enum VisualPortRole
{
    /// <summary>Execution flow enters this node through this port, usually on the top.</summary>
    FlowIn,

    /// <summary>Execution flow exits this node through this port, usually on the bottom.</summary>
    FlowOut,

    /// <summary>Data value is consumed by this node through this port, usually on the left.</summary>
    DataIn,

    /// <summary>Data value is produced by this node and emitted through this port, usually on the right.</summary>
    DataOut,

    /// <summary>
    /// Execution branches into a block body, such as conditional branch, loop body or case body through this port.
    /// </summary>
    /// <remarks>
    /// Distinguished from <see cref="FlowOut"/> so that <c>LinkFlow</c> always follows the main execution
    /// path rather than a branch, allowing block opener nodes to carry both a main-flow <see cref="FlowOut"/>
    /// to their closing node and one or more <see cref="BranchOut"/> ports to their body subgraphs.
    /// </remarks>
    BranchOut,
}
