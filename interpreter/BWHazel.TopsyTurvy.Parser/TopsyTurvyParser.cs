using System;
using System.Collections.Generic;
using System.Linq;
using Superpower;
using Superpower.Model;
using BWHazel.TopsyTurvy.Ast;
using static BWHazel.TopsyTurvy.Parser.ParserHelpers;

namespace BWHazel.TopsyTurvy.Parser;

/// <summary>
/// Public entry point for parsing a Topsy Turvy source file into an AST.
/// </summary>
/// <remarks>
/// <para>
/// The Topsy Turvy Parser is the top and main entry point for the Parser which builds on top of the lexer and expression and
/// statement parsers to build a single <see cref="ProgramNode"/> AST component.  It comprises a single parser for a programme,
/// which is made up of parsers from lower levels for statements and expressions.
/// </para>
/// <para>
/// Please note that for brevity the capture of spans and committed-parse semantics are not documented for each parser individually.  Please see the **Source Span Lifecycle** and **Committed Parse Semantics** sections below for descriptions of how spans are captured and how block-body parsers handle partial matches.
/// </para>
/// <para>
/// ### Programme Parser
/// The <c>ProgramParser</c> parser matches on a complete Topsy Turvy programme, returning a <see cref="ProgramNode"/> with the
/// programme title, optional subtitle and the list of top-level statements that form the programme body:
/// * It first matches the <c>HARK!</c> keyword that opens every programme.
/// * It then matches required whitespace followed by a string literal for the programme title.
/// * It tries to match an optional subtitle, back-tracking if not present:
///     * It matches required whitespace followed by the <c>or,</c> keyword.
///     * It then matches required whitespace followed by a string literal for the subtitle.
///     * If no subtitle is present, a default value of <c>null</c> is used.
/// * It then matches zero or more statements, each preceded by required whitespace, back-tracking on each attempt that does not match a known statement form.
/// * Finally, it matches the <c>FINALE.</c> keyword that closes every programme.
/// </para>
/// <para>
/// In the following Topsy Turvy examples:
/// <code>
/// ASIDE: Programme with subtitle.
/// HARK! "The Mikado"
///   or, "The Town of Titipu"
/// 
/// BEHOLD "The Lord High Executioner!"
/// 
/// FINALE.
/// 
/// ASIDE: Programme without subtitle.
/// HARK! "Patience"
/// 
/// PRAY WELCOME LovesickMaidens AS A PEER BEING 20
/// 
/// FINALE.
/// </code>
/// both would return a <see cref="ProgramNode"/> and for each example:
/// * The first would have the title <c>The Mikado</c>, a subtitle of <c>The Town of Titipu</c> and a single <c>BEHOLD</c> statement.
/// * The second would have the title <c>Patience</c>, no subtitle and a single declaration statement.
/// </para>
/// <para>
/// ### Source Span Lifecycle
/// Before calling any parser combinator, both the <see cref="Parse"/> and <see cref="TryParse"/> methods assign
/// <see cref="ParserHelpers.ActiveSourceMap"/> to the <see cref="SourceMap"/> produced by the pre-processor pipeline.
/// This thread-local field is the communication channel that allows the <c>static readonly</c> parser fields in
/// <see cref="ExpressionParser"/> and <see cref="StatementParser"/> to translate pre-processed cursor offsets back to
/// original source line and column pairs at node construction time via <see cref="ParserHelpers.BuildSpan"/>.
/// The field is cleared to <c>null</c> in a <c>finally</c> block after the parse completes so that it does not
/// persist into subsequent parses on the same thread.
/// </para>
/// <para>
/// ### Committed Parse Semantics
/// The <c>ProgramParser</c> uses committed parse semantics to match the sequence of top-level statements
/// in the programme body: once the opening keyword of a statement has been consumed,
/// any subsequent failure is propagated at the actual position of the error rather than
/// backtracking to before the whitespace that preceded the keyword.  As a result, syntax errors in top-level
/// statements are reported at the line where the error occurs, not at the start of the programme body.  Parsing
/// still stops at the first unrecoverable error: there is no multi-error recovery.  All block-body parsers
/// within <see cref="StatementParser"/> apply the same approach so please see its remarks for details.
/// </para>
/// <para>
/// ### Parsing Process
/// 2 methods are available for parsing a programme depending on how the caller wants to handle parsing errors:
/// * <see cref="Parse"/> is for callers that treat parse failures as fatal, such as an interpreter.
/// * <see cref="TryParse"/> is for callers that treat parse errors as data that needs to be inspected and acted upon, such as a code editor.
/// </para>
/// <para>
/// #### Parse (see <see cref="Parse"/>)
/// After running the <see cref="PreProcessorPipeline"/> it performs a complete parse.
/// * If parsing was successful it returns a <see cref="ProgramNode"/>.
/// * If parsing failed, or semantic errors are found even after a successful parse, it throws a <see cref="TopsyTurvySyntaxException"/> for:
///     * Syntax errors, passing the parse result.
///     * Semantic errors, such as reserved keywords used as variable or function names, passing identified issues.
/// </para>
/// <code>
/// TopsyTurvyParser parser = new();
/// string sourceCode = File.ReadAllText("programme.topsy");
/// try
/// {
///     ProgramNode program = parser.Parse(sourceCode);
///     // Process the program...
/// }
/// catch (TopsyTurvySyntaxException ex)
/// {
///     Console.WriteLine("Syntax error(s) found:");
///     foreach (string error in ex.Errors)
///     {
///         Console.WriteLine(error);
///     }
/// }
/// </code>
/// <para>
/// #### TryParse (see <see cref="TryParse"/>)
/// As with the <see cref="Parse"/> method, after running the <see cref="PreProcessorPipeline"/> it performs a complete parse.
/// * If parsing and semantic errors check was successful it returns a <see cref="ParseResult"/> with the <see cref="ParseResult.Program"/> property set to the parsed <see cref="ProgramNode"/> and an empty list of diagnostics.
/// * On a successful parse but a failed semantic check:
///     * It returns a <see cref="ParseResult"/> with the <see cref="ParseResult.Program"/> property set to the parsed <see cref="ProgramNode"/> and a list of diagnostics describing the semantic errors found.
/// * On a failed parse:
///     * It finds the start of the error based on the parse result, which reports the start of the invalid statement, by scanning forward over any whitespace.
///     * It then re-parses the invalid statement to get more specific error information if possible, again scanning forward over any whitespace to find the actual start of the error.
///     * It then finds the end of the line on which the error occurs, or the end of the programme if the error is on the last line, to determine the full span of the error.
///     * It then maps the error span back to the original source text using the source map from the pre-processor.
///     * It then builds an error message based on the parser errors.
///         * If the only error is related to a missing closing <c>FINALE.</c> keyword it builds a message that includes a snippet of the unexpected content.
///         * Otherwise it builds a message based on the parser error messages.
/// </para>
/// <code>
/// TopsyTurvyParser parser = new();
/// string sourceCode = File.ReadAllText("programme.topsy");
/// ParseResult result = parser.TryParse(sourceCode);
/// if (result.Success)
/// {
///     ProgramNode program = result.Program;
///     // Process the program...
/// }
/// else
/// {
///     Console.WriteLine("Syntax error(s) found:");
///     foreach (Diagnostic diagnostic in result.Diagnostics)
///     {
///         Console.WriteLine(diagnostic.Message);
///     }
/// }
/// </code>
/// </remarks>
public class TopsyTurvyParser
{
    private const int DefaultInvalidContentSnippetLength = 40;

    /// <summary>
    /// Every individual word that appears in any Topsy Turvy keyword.
    /// </summary>
    private static readonly HashSet<string> ReservedWords = new(StringComparer.OrdinalIgnoreCase)
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

    /// <summary>
    /// Top-level parser for a complete Topsy Turvy program.
    /// </summary>
    private static readonly TextParser<ProgramNode> ProgramParser =
        from startOffset in CurrentOffset
        from harkKeyword in Lexer.Keyword("HARK!")
        from title in Ws(Lexer.StringLiteral
            .Named("program title"))
        from subtitle in Ws(Lexer.Keyword("or,")
            .IgnoreThen(Ws(Lexer.StringLiteral)))
            .Try()
            .OptionalOrDefault(null!)
        from body in WsMany(StatementParser.Statement)
        from closer in Ws(Lexer.Keyword("FINALE.")
            .Named("FINALE. (program end)"))
        from endOffset in CurrentOffset
        select new ProgramNode()
        {
            Title = title,
            Subtitle = subtitle,
            Statements = body.ToList(),
            Span = BuildSpan(startOffset, endOffset)
        };

    /// <summary>
    /// Parses the supplied Topsy Turvy source text into a <see cref="ProgramNode"/>.
    /// </summary>
    /// <param name="source">The raw source code to parse.</param>
    /// <returns>The root AST node representing the complete programme.</returns>
    /// <exception cref="TopsyTurvySyntaxException">Thrown when the source text contains syntax errors.</exception>
    public ProgramNode Parse(string source)
    {
        PreProcessorPipeline preProcessorPipeline = BuildPreProcessorPipeline();
        PreProcessResult preProcessorResult = preProcessorPipeline.Execute(source);

        Result<ProgramNode> parseResult;
        ActiveSourceMap = preProcessorResult.SourceMap;
        try
        {
            parseResult = ProgramParser.TryParse(preProcessorResult.TransformedText);
        }
        finally
        {
            ActiveSourceMap = null;
        }

        if (!parseResult.HasValue)
        {
            throw new TopsyTurvySyntaxException([parseResult.ToString()]);
        }

        IReadOnlyList<Diagnostic> semanticErrors = ValidateSymbolNames(parseResult.Value);
        if (semanticErrors.Count > 0)
        {
            throw new TopsyTurvySyntaxException(semanticErrors.Select(diagnostic => diagnostic.Message).ToArray());
        }

        return parseResult.Value;
    }

        /// <summary>
    /// Attempts to parse the supplied Topsy Turvy source text.
    /// </summary>
    /// <param name="source">The raw source code to parse.</param>
    /// <remarks>
    /// <para>
    /// Returns a <see cref="ParseResult"/> that carries either the parsed programme or
    /// a structured diagnostic on failure.  The <see cref="ParseResult.Success"/> property
    /// is set to <c>true</c> and <see cref="ParseResult.Program"/> populated on success.
    /// Otherwise a single <see cref="Diagnostic"/> describing the syntax error with its
    /// source location is set.
    /// </para>
    /// <para>
    /// Additional offset processing is required as Superpower resets to before
    /// the <see cref="Ws{T}"/> call: the <c>\n</c> at the end of the previous
    /// statement, not the invalid line.  The processing skips past any whitespace
    /// to land on the actual first character of invalid content.
    /// </para>
    /// </remarks>
    /// <returns>
    /// A <see cref="ParseResult"/> with parsing results.
    /// </returns>
    public ParseResult TryParse(string source)
    {
        PreProcessorPipeline preProcessorPipeline = BuildPreProcessorPipeline();
        PreProcessResult preProcessorResult = preProcessorPipeline.Execute(source);

        Result<ProgramNode> programmeParseResult;
        ActiveSourceMap = preProcessorResult.SourceMap;
        try
        {
            programmeParseResult = ProgramParser.TryParse(preProcessorResult.TransformedText);
        }
        finally
        {
            ActiveSourceMap = null;
        }

        if (!programmeParseResult.HasValue)
        {
            // Search forward from reported error position, which is the start of the invalid statement, to find
            // the first non-whitespace character.
            string processedText = preProcessorResult.TransformedText;
            int errorSearchStartOffset = programmeParseResult.ErrorPosition.Absolute;
            while (errorSearchStartOffset < processedText.Length && char.IsWhiteSpace(processedText[errorSearchStartOffset]))
            {
                errorSearchStartOffset++;
            }

            // Re-parse the invalid statement to get more accurate position and message on the specific error within the statement.
            int errorOffset = errorSearchStartOffset;
            string[] errorExpectations = programmeParseResult.Expectations ?? [];
            if (errorSearchStartOffset < processedText.Length)
            {
                Result<Statement> errorResult = StatementParser.Statement.TryParse(processedText[errorSearchStartOffset..]);
                if (!errorResult.HasValue && errorResult.ErrorPosition.Absolute > 0)
                {
                    int specificErrorOffset = errorSearchStartOffset + errorResult.ErrorPosition.Absolute;
                    while (specificErrorOffset < processedText.Length && char.IsWhiteSpace(processedText[specificErrorOffset]))
                    {
                        specificErrorOffset++;
                    }

                    errorOffset = specificErrorOffset;
                    errorExpectations = errorResult.Expectations ?? [];
                }
            }

            // Find span for the error, which is from the first non-whitespace character of the invalid statement to the end of
            // the line or end of the programme, whichever comes first.
            int lineEndOffset = errorOffset;
            while (lineEndOffset < processedText.Length
                && processedText[lineEndOffset] != '\n'
                && processedText[lineEndOffset] != '\r')
            {
                lineEndOffset++;
            }

            // Map the error offsets to the original source locations using the source map from the pre-processor.
            SourceLocation startLocation = preProcessorResult.SourceMap.GetOriginalLocation(errorOffset);
            SourceLocation endLocation;
            if (lineEndOffset > errorOffset)
            {
                SourceLocation endLocationTemp = preProcessorResult.SourceMap.GetOriginalLocation(lineEndOffset - 1);
                endLocation = new(endLocationTemp.Line, endLocationTemp.Column + 1);
            }
            else
            {
                endLocation = new(startLocation.Line, startLocation.Column + 1);
            }

            SourceSpan span = new(startLocation, endLocation);

            // Check if the parser expected only FINALE. but found unexpected content, and build error
            // message that includes the unexpected content.
            bool onlyExpectsFinale =
                errorExpectations.Length > 0 &&
                errorExpectations.All(expectation => expectation.Contains("FINALE"));
            bool atEndOfFile = errorOffset >= processedText.Length;

            string message;
            if (onlyExpectsFinale && !atEndOfFile)
            {
                // Build error message for missing FINALE. error.
                string invalidLineContent = processedText[errorOffset..lineEndOffset].Trim();
                string snippet = invalidLineContent.Length > DefaultInvalidContentSnippetLength
                    ? invalidLineContent[..DefaultInvalidContentSnippetLength] + "..."
                    : invalidLineContent;

                message = snippet.Length > 0
                    ? $"Unexpected: {snippet}"
                    : "Syntax error";
            }
            else
            {
                // Build generic error message based on parser errors.
                message = !string.IsNullOrEmpty(programmeParseResult.ErrorMessage)
                    ? programmeParseResult.ErrorMessage
                    : errorExpectations.Length > 0
                        ? $"Expected: {string.Join(", ", errorExpectations)}"
                        : "Syntax error";
            }

            Diagnostic diagnostic = new(message, DiagnosticSeverity.Error, span);
            return new(null, [diagnostic]);
        }

        IReadOnlyList<Diagnostic> semanticErrors = ValidateSymbolNames(programmeParseResult.Value);
        if (semanticErrors.Count > 0)
        {
            return new(programmeParseResult.Value, semanticErrors);
        }

        return new(programmeParseResult.Value, []);
    }

    /// <summary>
    /// Builds the pre-processor pipeline used to transform the raw source text before parsing.
    /// </summary>
    /// <returns>The configured pre-processor pipeline.</returns>
    private static PreProcessorPipeline BuildPreProcessorPipeline()
    {
        PreProcessorPipeline pipeline = new();
        pipeline.AddProcessor(new CommentsPreProcessor());
        pipeline.AddProcessor(new VictorianFlourishPreProcessor());
        return pipeline;
    }

    /// <summary>
    /// Validates that no variable or function name in the programme uses a reserved keyword.
    /// </summary>
    /// <param name="program">The parsed programme AST.</param>
    /// <returns>A list of diagnostics, one per violation, empty when all names are valid.</returns>
    private static IReadOnlyList<Diagnostic> ValidateSymbolNames(ProgramNode program)
    {
        List<Diagnostic> diagnostics = [];

        foreach (Statement statement in program.Statements)
        {
            switch (statement)
            {
                case PrincipalBlockNode principalBlock:
                    foreach (Statement declaration in principalBlock.Declarations)
                    {
                        if (declaration is DeclarationNode scalarDeclaration)
                        {
                            CheckDeclarationName(scalarDeclaration.Name, "variable", scalarDeclaration.Span, diagnostics);
                        }
                        else if (declaration is ArrayDeclarationNode arrayDeclaration)
                        {
                            CheckDeclarationName(arrayDeclaration.Name, "variable", arrayDeclaration.Span, diagnostics);
                        }
                    }

                    break;
                case DeclarationNode declaration:
                    CheckDeclarationName(declaration.Name, "variable", declaration.Span, diagnostics);
                    break;
                case FunctionDefinitionNode functionDefinition:
                    CheckDeclarationName(functionDefinition.Name, "function", functionDefinition.Span, diagnostics);
                    foreach ((string parameter, SourceSpan paramSpan) in
                        functionDefinition.Parameters.Zip(functionDefinition.ParameterSpans))
                    {
                        CheckDeclarationName(parameter, "parameter", paramSpan, diagnostics);
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
    /// <param name="kind">Either <c>variable</c>, <c>function</c>, or <c>parameter</c>, used in the error message.</param>
    /// <param name="span">The source span of the declaration, taken directly from the AST node.</param>
    /// <param name="diagnostics">The diagnostic list to append to on a violation.</param>
    private static void CheckDeclarationName(string name, string kind, SourceSpan span, List<Diagnostic> diagnostics)
    {
        if (!ReservedWords.Contains(name))
        {
            return;
        }

        string errorMessage = $"'{name}' is a reserved keyword and cannot be used as a {kind} name.";
        diagnostics.Add(new(errorMessage, DiagnosticSeverity.Error, span));
    }
}
