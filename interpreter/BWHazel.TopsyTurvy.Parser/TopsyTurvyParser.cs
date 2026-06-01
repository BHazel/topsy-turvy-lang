using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

    /// <summary>
    /// Every individual word that appears in any Topsy Turvy keyword.
    /// </summary>
    private static readonly HashSet<string> ReservedWords = new(System.StringComparer.OrdinalIgnoreCase)
    {
        "A", "ABOVE", "ACTING", "ADMIT", "ALIKE", "ALL", "AND", "ANY", "APPOINTED", "AS", "ASCENDING",
        "BE", "BEING", "BEHOLD", "BOTH", "BY",
        "CAPACITY", "CEREMONY", "CONCLUDES", "COULD", "CURSE", "CURTAIN",
        "DECREE", "DEGREE", "DESCENDING", "DIFFERENCE", "DISCHARGED", "DO", "DUTY",
        "EITHER", "EVER", "EXPIRES",
        "FAILING", "FATHOM", "FICTION", "FINALE", "FIND", "FOR",
        "GRATITUDE", "GREATEST",
        "HARDLY", "HARK", "HENCEFORTH", "HIDEOUS",
        "I", "IF", "IN", "IS", "IT",
        "JUST", "KNOWN",
        "LARGER", "LEGAL", "LOWER",
        "MATTER", "MODIFIED", "MORE", "MUCH", "MY",
        "NAUGHT", "NAY", "NO", "NOT", "NOTHING",
        "OBLIGATION", "OF", "ON", "ONCE", "OR", "OTHERWISE",
        "PEER", "PERFORM", "PLEASE", "PRE-ADAMITE", "PREMATURELY", "PRINCIPALS", "PRODUCT",
        "QUITE", "QUOTIENT",
        "REMAINDER", "RAPTURE", "RESPECT", "RISES",
        "SATISFACTORY", "SHOULD", "SMALLER", "SO", "SUM", "SUMMON",
        "TELL", "TERM", "TERMS", "THAT", "THE", "TRANSPIRE",
        "UNDER", "UNLIKE", "UNTIL",
        "VERITY",
        "WELCOME", "WERE", "WHILST", "WHEN", "WITH", "WITHOUT", "WILL", "WOVEN",
        "YARN", "YOU",
    };

    private static TextParser<T> Ws<T>(TextParser<T> parser) =>
        Lexer.WhitespaceRequired.IgnoreThen(parser);

    /// <summary>
    /// Top-level parser for a complete Topsy Turvy program.
    /// </summary>
    private static readonly TextParser<ProgramNode> ProgramParser =
        from start in Lexer.Keyword("HARK!")
        from title in Ws(Lexer.StringLiteral.Named("program title"))
        from subtleOpt in Ws(Lexer.Keyword("or,").IgnoreThen(Ws(Lexer.StringLiteral)))
                           .Try().OptionalOrDefault(null!)
        from body in Ws(StatementParser.Statement).Try().Many()
        from end in Ws(Lexer.Keyword("FINALE.").Named("FINALE. (program end)"))
        select new ProgramNode
        {
            Title = title,
            Subtitle = subtleOpt,
            Statements = body.ToList(),
            Span = PlaceholderSpan
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

        IReadOnlyList<Diagnostic> semanticErrors = ValidateSymbolNames(result.Value, source);
        if (semanticErrors.Count > 0)
        {
            throw new TopsyTurvySyntaxException(semanticErrors.Select(d => d.Message).ToArray());
        }

        return result.Value;
    }

    /// <summary>
    /// Validates that no variable or function name in the programme uses a reserved keyword.
    /// </summary>
    /// <param name="program">The parsed programme AST.</param>
    /// <param name="originalSource">The original (pre-processed) source text, used for span recovery.</param>
    /// <returns>A list of diagnostics, one per violation, empty when all names are valid.</returns>
    private static IReadOnlyList<Diagnostic> ValidateSymbolNames(ProgramNode program, string originalSource)
    {
        List<Diagnostic> diagnostics = [];
        string[] sourceLines = originalSource.Split('\n');

        foreach (Statement statement in program.Statements)
        {
            switch (statement)
            {
                case PrincipalBlockNode principalBlock:
                    foreach (DeclarationNode declaration in principalBlock.Declarations)
                    {
                        CheckDeclarationName(declaration.Name, "variable", sourceLines, diagnostics);
                    }

                    break;
                case DeclarationNode declaration:
                    CheckDeclarationName(declaration.Name, "variable", sourceLines, diagnostics);
                    break;
                case FunctionDefinitionNode functionDefinition:
                    CheckDeclarationName(functionDefinition.Name, "function", sourceLines, diagnostics);
                    foreach (string parameter in functionDefinition.Parameters)
                    {
                        CheckDeclarationName(parameter, "parameter", sourceLines, diagnostics);
                    }

                    break;
            }
        }

        return diagnostics;
    }

    /// <summary>
    /// Checks whether a declared name is a reserved keyword and, if so, appends a diagnostic.
    /// </summary>
    /// <param name="name">The declared name to check.</param>
    /// <param name="kind">Either <c>variable</c> or <c>function</c>, used in the error message.</param>
    /// <param name="sourceLines">The original source split into lines, for span recovery.</param>
    /// <param name="diagnostics">The diagnostic list to append to on a violation.</param>
    private static void CheckDeclarationName(
        string name,
        string kind,
        string[] sourceLines,
        List<Diagnostic> diagnostics)
    {
        if (!ReservedWords.Contains(name))
        {
            return;
        }

        string message = $"'{name}' is a reserved keyword and cannot be used as a {kind} name.";
        SourceSpan span = FindDeclarationSpan(name, kind, sourceLines);
        diagnostics.Add(new(message, DiagnosticSeverity.Error, span));
    }

    /// <summary>
    /// Scans the source lines for a declaration and returns its span.
    /// </summary>
    /// <param name="name">The declared name to locate.</param>
    /// <param name="kind">Either <c>variable</c> or <c>function</c>.</param>
    /// <param name="sourceLines">The original source split into lines.</param>
    /// <returns>A <see cref="SourceSpan"/> covering the name token, or a fallback span at (1,1).</returns>
    private static SourceSpan FindDeclarationSpan(string name, string kind, string[] sourceLines)
    {
        string pattern = kind switch
        {
            "function" => $@"(?i)\bIT\s+IS\s+MY\s+DUTY\s+TO\s+PERFORM\s+({Regex.Escape(name)})\b",
            "parameter" => $@"(?i)\bUNDER\s+THE\s+TERMS\s+OF\b.*\b({Regex.Escape(name)})\b",
            _ => $@"(?i)\bPRAY\s+WELCOME\s+({Regex.Escape(name)})\b",
        };

        for (int lineIndex = 0; lineIndex < sourceLines.Length; lineIndex++)
        {
            Match match = Regex.Match(sourceLines[lineIndex], pattern);
            if (match.Success)
            {
                int line = lineIndex + 1;
                int startColumn = match.Groups[1].Index + 1;
                int endColumn = startColumn + name.Length;
                return new SourceSpan(new SourceLocation(line, startColumn), new SourceLocation(line, endColumn));
            }
        }

        return new(new SourceLocation(1, 1), new SourceLocation(1, 2));
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

        IReadOnlyList<Diagnostic> semanticErrors = ValidateSymbolNames(result.Value, source);
        if (semanticErrors.Count > 0)
        {
            return new ParseResult(result.Value, semanticErrors);
        }

        return new(result.Value, []);
    }
}
