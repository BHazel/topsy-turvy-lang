using System.Collections.Generic;
using System.Linq;
using Superpower;
using Superpower.Model;
using BWHazel.TopsyTurvy.Ast;

namespace BWHazel.TopsyTurvy.Parser;

/// <summary>
/// Public entry point for parsing a Topsy Turvy source file into an AST.
/// </summary>
public class TopsyTurvyParser
{
    private static readonly SourceSpan PlaceholderSpan = new(new(0, 0), new(0, 0));

    private static TextParser<T> Ws<T>(TextParser<T> parser) =>
        Lexer.WhitespaceRequired.IgnoreThen(parser);

    /// <summary>
    /// Top-level parser for a complete Topsy Turvy program.
    /// </summary>
    private static readonly TextParser<ProgramNode> ProgramParser =
        from start     in Lexer.Keyword("HARK!")
        from title     in Ws(Lexer.StringLiteral.Named("program title"))
        from subtleOpt in Ws(Lexer.Keyword("or,").IgnoreThen(Ws(Lexer.StringLiteral)))
                           .Try().OptionalOrDefault(null!)
        from body      in Ws(StatementParser.Statement).Try().Many()
        from end       in Ws(Lexer.Keyword("FINALE.").Named("FINALE. (program end)"))
        select new ProgramNode
            {
                Title      = title,
                Subtitle   = subtleOpt,
                Statements = body.ToList(),
                Span       = PlaceholderSpan
            };

    /// <summary>
    /// Parses the supplied Topsy Turvy source text into a <see cref="ProgramNode"/>.
    /// </summary>
    /// <param name="source">The raw source code to parse.</param>
    /// <returns>The root AST node representing the complete programme.</returns>
    /// <exception cref="TopsyTurvySyntaxException">Thrown when the source text contains syntax errors.</exception>
    public ProgramNode Parse(string source)
    {
        PreProcessorPipeline pipeline = new();
        pipeline.AddProcessor(new CommentsPreProcessor());
        pipeline.AddProcessor(new VictorianFlourishPreProcessor());
        PreProcessResult processed = pipeline.Execute(source);

        Result<ProgramNode> result = ProgramParser.TryParse(processed.Text);
        if (!result.HasValue)
        {
            throw new TopsyTurvySyntaxException([result.ToString()]);
        }

        return result.Value;
    }

    /// <summary>
    /// Attempts to parse the supplied Topsy Turvy source text.
    /// </summary>
    /// <param name="source">The raw source code to parse.</param>
    /// <remarks>
    /// <para>
    /// Returns a <see cref="ParseResult"/> that carries either the parsed programme or
    /// a structured diagnostic on failure.  The <see cref="ParseResult.Success"/> property
    /// ia set to <c>true</c> and <see cref="ParseResult.Program"/> populated on success.
    /// Otherwise a single <see cref="Diagnostic"/> describing the syntax error with its
    /// source location is set.
    /// </para>
    /// <para>
    /// Additional offset processing is required as Superpower resets to before
    /// the <see cref="Ws{T}"/> call: the <code>\n</code> at the end of the previous
    /// statement, not the invalid line.  The processing skips past any whitespace
    /// to land on the actual first character of invalid content.
    /// </para>
    /// </remarks>
    /// <returns>
    /// A <see cref="ParseResult"/> with parsing results.
    /// </returns>
    public ParseResult TryParse(string source)
    {
        PreProcessorPipeline pipeline = new();
        pipeline.AddProcessor(new CommentsPreProcessor());
        pipeline.AddProcessor(new VictorianFlourishPreProcessor());
        PreProcessResult processed = pipeline.Execute(source);

        Result<ProgramNode> result = ProgramParser.TryParse(processed.Text);
        if (!result.HasValue)
        {
            string processedText = processed.Text;

            int startOffset = result.ErrorPosition.Absolute;
            while (startOffset < processedText.Length && char.IsWhiteSpace(processedText[startOffset]))
            {
                startOffset++;
            }

            int errorOffset = startOffset;
            string[] errorExpectations = result.Expectations;
            if (startOffset < processedText.Length)
            {
                Result<Statement> innerResult = StatementParser.Statement.TryParse(processedText.Substring(startOffset));
                if (!innerResult.HasValue && innerResult.ErrorPosition.Absolute > 0)
                {
                    int innerAbsolute = startOffset + innerResult.ErrorPosition.Absolute;
                    while (innerAbsolute < processedText.Length && char.IsWhiteSpace(processedText[innerAbsolute]))
                    {
                        innerAbsolute++;
                    }

                    errorOffset = innerAbsolute;
                    errorExpectations = innerResult.Expectations;
                }
            }

            int lineEndOffset = errorOffset;
            while (lineEndOffset < processedText.Length
                   && processedText[lineEndOffset] != '\n'
                   && processedText[lineEndOffset] != '\r')
            {
                lineEndOffset++;
            }

            (int startLine, int startColumn) = processed.SourceMap.GetOriginalLocation(errorOffset);
            SourceLocation startLocation = new(startLine, startColumn);

            SourceLocation endLocation;
            if (lineEndOffset > errorOffset)
            {
                (int endLine, int endColumn) = processed.SourceMap.GetOriginalLocation(lineEndOffset - 1);
                endLocation = new(endLine, endColumn + 1);
            }
            else
            {
                endLocation = new(startLine, startColumn + 1);
            }

            SourceSpan span = new(startLocation, endLocation);

            bool onlyExpectsFinale = errorExpectations.Length > 0
                && errorExpectations.All(expectation => expectation.Contains("FINALE"));
            bool atEndOfFile = errorOffset >= processedText.Length;

            string message;
            if (onlyExpectsFinale && !atEndOfFile)
            {
                string lineContent = processedText.Substring(errorOffset, lineEndOffset - errorOffset).Trim();
                string snippet = lineContent.Length > 40
                    ? lineContent.Substring(0, 40) + "..."
                    : lineContent;
                
                message = snippet.Length > 0
                    ? $"Unexpected: {snippet}"
                    : "Syntax error";
            }
            else
            {
                message = !string.IsNullOrEmpty(result.ErrorMessage)
                    ? result.ErrorMessage
                    : errorExpectations.Length > 0
                        ? $"Expected: {string.Join(", ", errorExpectations)}"
                        : "Syntax error";
            }

            Diagnostic diagnostic = new(message, DiagnosticSeverity.Error, span);
            return new ParseResult(null, [diagnostic]);
        }

        return new(result.Value, []);
    }
}
