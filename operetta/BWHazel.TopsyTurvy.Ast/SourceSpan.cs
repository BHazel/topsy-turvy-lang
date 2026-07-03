namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Represents a range of source code between a start and end location.
/// </summary>
/// <remarks>
/// <para>
/// A <see cref="SourceSpan"/> bounds a contiguous region of a Topsy Turvy source file between a <see cref="Start"/>
/// and <see cref="End"/> <see cref="SourceLocation"/>s.  The span is **exclusive**, meaning the start location is
/// included in the span but the end location is 1 column past the final character of the span.  Every <see cref="Node"/>
/// carries a <see cref="SourceSpan"/> via its <see cref="Node.Span"/> property, and every <see cref="Diagnostic"/>
/// identifies the region for an issue in source code through a span.  This allows the parser, runtime, and LSP to report
/// errors at the correct position in the source text.
/// </para>
/// <para>
/// For example, a span starting at line 3, character (column) 6 and ending at line 5, character 10 of a source file would be represented as:
/// </para>
/// <code>
/// new SourceSpan(
///     Start: new SourceLocation(Line: 3, Column: 6),
///     End: new SourceLocation(Line: 5, Column: 10)
/// )
/// </code>
/// <para>
/// There is no Topsy Turvy syntax that maps directly to <see cref="SourceSpan"/>.  It is an infrastructure
/// type used for diagnostic reporting and is never directly instantiated by user code.
/// </para>
/// </remarks>
/// <param name="Start">The starting location of the span.</param>
/// <param name="End">The ending location of the span.</param>
public record SourceSpan(SourceLocation Start, SourceLocation End);
