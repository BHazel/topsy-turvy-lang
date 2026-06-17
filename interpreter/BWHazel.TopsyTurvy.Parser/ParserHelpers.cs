using Superpower;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Parser;

/// <summary>
/// Provides shared helper members for use across the Topsy Turvy parser components.
/// </summary>
internal static class ParserHelpers
{
    /// <summary>
    /// A placeholder source span assigned to every AST node until accurate source-position wiring is implemented.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Source positions are not yet wired through the parser, so every AST node receives this value rather than
    /// the real line and column at which it appeared in the source file.  Both coordinates are set to <c>(0, 0)</c>,
    /// which is deliberately out of the valid range and allows downstream consumers to detect that the span has not
    /// yet been populated.
    /// </para>
    /// <para>
    /// Once source-position wiring is complete, this constant will be removed and replaced with spans derived from
    /// the actual parser cursor positions.
    /// </para>
    /// </remarks>
    internal static readonly SourceSpan PlaceholderSpan =
        new(new SourceLocation(0, 0), new SourceLocation(0, 0));

    /// <summary>
    /// Returns a parser that first consumes required whitespace, then runs <paramref name="parser"/>.
    /// </summary>
    /// <typeparam name="T">The result type of the wrapped parser.</typeparam>
    /// <param name="parser">The parser to run after the whitespace has been consumed.</param>
    /// <returns>
    /// A <see cref="TextParser{T}"/> that fails if no whitespace is present, then delegates to
    /// <paramref name="parser"/>.
    /// </returns>
    /// <remarks>
    /// This is a convenience wrapper around <c>WhitespaceRequired</c> parser on the Lexer and
    /// <c>Combinators.IgnoreThen{T,U}</c> that keeps multi-step LINQ parser expressions
    /// readable by avoiding repeated inline calls.
    /// </remarks>
    internal static TextParser<T> Ws<T>(TextParser<T> parser) =>
        Lexer.WhitespaceRequired.IgnoreThen(parser);
}
