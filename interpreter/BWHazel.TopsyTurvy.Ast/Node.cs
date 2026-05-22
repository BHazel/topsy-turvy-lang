namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Base class for all nodes in the Topsy Turvy Abstract Syntax Tree.
/// </summary>
public abstract class Node
{
    /// <summary>
    /// Gets or initialises the span of the source code that generated this node.
    /// </summary>
    public required SourceSpan Span { get; init; }
}
