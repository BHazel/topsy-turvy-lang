using System;
using System.Collections.Generic;
using Superpower;
using Superpower.Model;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Parser;

/// <summary>
/// Provides shared helper members for use across the Topsy Turvy parser components.
/// </summary>
internal static class ParserHelpers
{
    /// <summary>
    /// A placeholder source span used as a fallback when <see cref="ActiveSourceMap"/> is not set.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Both coordinates are set to <c>(0, 0)</c>, which is deliberately out of the valid 1-indexed range.
    /// This value is returned by <see cref="BuildSpan"/> when no <see cref="SourceMap"/> is active, for example
    /// in isolated tests that call a parser combinator directly without going through <see cref="TopsyTurvyParser"/>.
    /// </para>
    /// </remarks>
    internal static readonly SourceSpan PlaceholderSpan =
        new(new SourceLocation(0, 0), new SourceLocation(0, 0));

    /// <summary>
    /// Thread-local <see cref="SourceMap"/> set by <see cref="TopsyTurvyParser"/> before each parse call and
    /// cleared in a <c>finally</c> block after the parse completes.
    /// </summary>
    /// <remarks>
    /// The Topsy Turvy parsers are <c>static readonly</c> fields and cannot receive a <see cref="SourceMap"/>
    /// as a constructor argument.  This thread-local acts as the communication channel between
    /// <see cref="TopsyTurvyParser"/> and the static parser combinators so that <see cref="BuildSpan"/> can
    /// translate pre-processed absolute offsets back to original source line and column pairs at node construction time.
    /// Using <see cref="ThreadStaticAttribute"/> makes the field safe for parallel parses on different threads.
    /// </remarks>
    [ThreadStatic]
    internal static SourceMap? ActiveSourceMap;

    /// <summary>
    /// A parser that returns the current absolute character offset in the pre-processed text without consuming any input.
    /// </summary>
    /// <remarks>
    /// This parser should be inserted at the start and end of a LINQ combinator chain to bracket the characters consumed by that
    /// chain.  The two captured offsets can be passed to <see cref="BuildSpan"/> to obtain the <see cref="SourceSpan"/> for the
    /// AST node produced by the chain.
    /// </remarks>
    internal static readonly TextParser<int> CurrentOffset =
        input => Result.Value(input.Position.Absolute, input, input);

    /// <summary>
    /// Converts two pre-processed absolute character offsets to a <see cref="SourceSpan"/> via the active <see cref="SourceMap"/>.
    /// </summary>
    /// <param name="startOffset">Zero-based absolute offset of the first character of the span in the pre-processed text.</param>
    /// <param name="endOffset">Zero-based absolute offset one past the last character of the span in the pre-processed text.</param>
    /// <returns>
    /// A <see cref="SourceSpan"/> with line and column positions mapped back to the original source, or
    /// <see cref="PlaceholderSpan"/> when <see cref="ActiveSourceMap"/> is <c>null</c>.
    /// </returns>
    internal static SourceSpan BuildSpan(int startOffset, int endOffset) =>
        ActiveSourceMap is not null
            ? new(ActiveSourceMap.GetOriginalLocation(startOffset), ActiveSourceMap.GetOriginalLocation(endOffset))
            : PlaceholderSpan;

    /// <summary>
    /// Wraps a parser so that the result is returned together with the start and end absolute offsets
    /// of the consumed input.
    /// </summary>
    /// <typeparam name="T">The result type of the inner parser.</typeparam>
    /// <param name="parser">The parser whose input range is to be captured.</param>
    /// <returns>
    /// A <see cref="TextParser{T}"/> that returns a tuple of the inner parser value and the start and end offsets.
    /// </returns>
    /// <remarks>
    /// Normally it is preferred to insert <see cref="CurrentOffset"/> directly into a LINQ <c>from</c> chain when the chain is
    /// already written in LINQ style.  This extension should be used when a <c>.Select()</c> chain would otherwise require
    /// converting to LINQ solely to capture position.
    /// </remarks>
    internal static TextParser<(T Value, int StartOffset, int EndOffset)> WithOffsets<T>(this TextParser<T> parser) =>
        from startOffset in CurrentOffset
        from value in parser
        from endOffset in CurrentOffset
        select (value, startOffset, endOffset);

    /// <summary>
    /// Parses zero or more whitespace-prefixed occurrences of the specified parser using committed-parse semantics.
    /// </summary>
    /// <typeparam name="T">The result type of the inner parser.</typeparam>
    /// <param name="parser">The parser to run after each whitespace sequence.</param>
    /// <returns>
    /// A <see cref="TextParser{T}"/> that accumulates results until whitespace is absent or
    /// <paramref name="parser"/> fails without consuming input.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Unlike <c>Ws(parser).Try().Many()</c>, a partial match from <paramref name="parser"/>, where
    /// the opening keyword was consumed but the body failed, is propagated as an error at the actual
    /// failure position rather than being discarded.  The loop terminates normally when no whitespace is
    /// present or when <paramref name="parser"/> fails without consuming any input, i.e. the end-of-block
    /// closing keyword is next in the input.
    /// </para>
    /// <para>
    /// Partial-match detection compares the parser match cursor position to the position passed to
    /// <paramref name="parser"/>: if the remainder advanced, the parser consumed input before failing
    /// and the error is propagated.  If the remainder is unchanged, the parser matched nothing and the
    /// loop terminates normally.
    /// </para>
    /// </remarks>
    internal static TextParser<T[]> WsMany<T>(TextParser<T> parser) =>
        input =>
        {
            List<T> results = [];
            TextSpan remainder = input;
            while (true)
            {
                // Look for whitespae: if there is none then the end of a block has been reached
                // so the loop is terminated.
                Result<char[]> whitespaceRequiredResult = Lexer.WhitespaceRequired(remainder);
                if (!whitespaceRequiredResult.HasValue)
                {
                    break;
                }

                // Run the parser after the whitespace.
                // It is important to note that the cursor position after the whitespace is saved
                // separately from the cursor position after the parser, held in <c>remainder</c>.
                Result<T> parserResult = parser(whitespaceRequiredResult.Remainder);
                if (parserResult.HasValue)
                {
                    // Successful Parse: No errors so add the result to the list, update <c>remainder</c>
                    // to the new cursor position and continue the loop.
                    results.Add(parserResult.Value);
                    remainder = parserResult.Remainder;
                }
                else if (parserResult.Remainder != whitespaceRequiredResult.Remainder)
                {
                    // Partial Match: The parser consumed some input but then failed as the cursor position
                    // advanced.  This means a keyword was recognised but the rest of the statement was
                    // invalid and the error positiion is returned immediately.  The cast is to satisfy the
                    // return type of the method.
                    return Result.CastEmpty<T, T[]>(parserResult);
                }
                else
                {
                    // No Match: The parser did not consume any input so the loop is terminated.
                    break;
                }
            }
            
            // Return all successful results.
            return Result.Value(results.ToArray(), input, remainder);
        };

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
