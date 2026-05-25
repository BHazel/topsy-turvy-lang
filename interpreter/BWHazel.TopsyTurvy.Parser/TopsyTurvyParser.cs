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
        from title     in Ws(Lexer.StringLiteral)
        from subtleOpt in Ws(Lexer.Keyword("or,").IgnoreThen(Ws(Lexer.StringLiteral)))
                           .Try().OptionalOrDefault(null!)
        from body      in Ws(StatementParser.Statement).Try().Many()
        from end       in Ws(Lexer.Keyword("FINALE."))
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
    /// Returns a <see cref="ParseResult"/> that carries either the parsed programme or
    /// a structured diagnostic on failure.  The <see cref="ParseResult.Success"/> property
    /// ia set to <c>true</c> and <see cref="ParseResult.Program"/> populated on success.
    /// Otherwise a single <see cref="Diagnostic"/> describing the syntax error with its
    /// source location is set.
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
            SourceLocation location = new(result.ErrorPosition.Line, result.ErrorPosition.Column);
            SourceSpan span = new(location, location);
            Diagnostic diagnostic = new(result.ErrorMessage, DiagnosticSeverity.Error, span);
            return new ParseResult(null, [diagnostic]);
        }

        return new(result.Value, []);
    }
}
