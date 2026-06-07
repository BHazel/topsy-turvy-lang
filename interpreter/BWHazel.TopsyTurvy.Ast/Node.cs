namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Base class for all nodes in the Topsy Turvy Abstract Syntax Tree.
/// </summary>
/// <remarks>
/// <para>
/// Every element in a parsed Topsy Turvy programme, whether a statement that performs an action or an expression that
/// produces a value, is represented as a <see cref="Node"/>.  The two direct subtypes are <see cref="Statement"/> and
/// <see cref="Expression"/>.  Every node carries a <see cref="Span"/> recording the position in the source text where it
/// originated, which is used for diagnostic reporting.
/// </para>
/// <para>
/// There is no Topsy Turvy syntax that maps directly to <see cref="Node"/>.  It is a base class only and is never
/// directly instantiated.
/// </para>
/// </remarks>
public abstract class Node
{
    /// <summary>
    /// Gets or initialises the span of the source code that generated this node.
    /// </summary>
    public required SourceSpan Span { get; init; }
}
