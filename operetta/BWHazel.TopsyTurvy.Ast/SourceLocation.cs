namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a specific location in the source code.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="SourceLocation"/> identifies a single point in a Topsy Turvy source file by its 1-indexed
/// <see cref="Line"/> and <see cref="Column"/> numbers.  Two locations together form a <see cref="SourceSpan"/>
/// bounding a region of source text, which is carried by every <see cref="Node"/> and <see cref="Diagnostic"/>.
/// </para>
/// <para>
/// For example, a location of line 3 and character (column) 6 of a source file would be represented as:
/// </para>
/// <code>
/// new SourceLocation(Line: 3, Column: 6)
/// </code>
/// <para>
/// There is no Topsy Turvy syntax that maps directly to <see cref="SourceLocation"/>.  It is an infrastructure
/// type used for diagnostic reporting and is never directly instantiated by user code.
/// </para>
/// </remarks>
/// <param name="Line">The 1-indexed line number.</param>
/// <param name="Column">The 1-indexed column number.</param>
public record SourceLocation(int Line, int Column);
