namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Base class for all nodes in the Topsy Turvy Abstract Syntax Tree.
/// </summary>
public abstract class TopsyTurvyNode
{
    /// <summary>
    /// Gets or initialises the span of the source code that generated this node.
    /// </summary>
    public required TopsyTurvySourceSpan Span { get; init; }
}
