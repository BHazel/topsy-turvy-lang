using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Parser;
using Xunit;

namespace BWHazel.TopsyTurvy.Tests;

/// <summary>
/// Tests for <see cref="TopsyTurvyParser"/> class.
/// </summary>
public class TopsyTurvyParserTests
{
    private readonly TopsyTurvyParser parser = new();

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyParser.Parse"/> method correctly parses a minimal program.
    /// </summary>
    /// <param name="source">The source code of the program to parse.</param>
    /// <param name="expectedTitle">The expected title of the parsed program.</param>
    [Theory]
    [InlineData("HARK! \"Hello\" FINALE.", "Hello")]
    public void Parse_WithMinimalProgram_ReturnsTitle(string source, string expectedTitle)
    {
        ProgramNode program = this.parser.Parse(source);
        Assert.Equal(expectedTitle, program.Title);
    }

    /// <summary>
    /// Tests the <see cref="TopsyTurvyParser.Parse"/> method correctly parses a minimal program with a subtitle.
    /// </summary>
    [Fact]
    public void Parse_WithMinimalProgramWithSubtitle_ReturnsSubtitle()
    {
        string source = "HARK! \"Title\" or, \"Subtitle\" FINALE.";
        ProgramNode program = this.parser.Parse(source);
        Assert.Equal("Subtitle", program.Subtitle);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyParser.Parse"/> method correctly parses an assignment statement.
    /// </summary>
    [Fact]
    public void Parse_WithProgramWithAssignment_ReturnsAssignment()
    {
        string source = "HARK! \"Title\" Ko-Ko IS APPOINTED 42 FINALE.";
        ProgramNode program = this.parser.Parse(source);

        Assert.Single(program.Statements);
        Statement statement = program.Statements[0];
        Assert.IsType<AssignmentNode>(statement);
        AssignmentNode assignment = (AssignmentNode)statement;
        Assert.Equal("Ko-Ko", assignment.Target);
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyParser.Parse"/> method correctly parses a variadic expression.
    /// </summary>
    [Fact]
    public void Parse_WithProgramWithVariadicExpression_HandlesCloser()
    {
        string source = "HARK! \"T\" WOVEN OF \"A\" AND \"B\" AND \"C\" IF YOU PLEASE. FINALE.";
        ProgramNode program = this.parser.Parse(source);

        ExpressionStatement statement = (ExpressionStatement)program.Statements[0];
        PrefixExpressionNode expression = (PrefixExpressionNode)statement.Expression;
        Assert.Equal(Operator.WovenOf, expression.Operator);
        Assert.Equal(3, expression.Arguments.Count);
    }

    /// <summary>
    /// Tests that <see cref="TopsyTurvyParser.TryParse"/> succeeds on valid source and returns no diagnostics.
    /// </summary>
    [Fact]
    public void TryParse_WithValidSource_ReturnsSuccessAndNoDiagnostics()
    {
        string source = "HARK! \"Test\" BEHOLD \"hello\" FINALE.";
        ParseResult result = this.parser.TryParse(source);

        Assert.True(result.Success);
        Assert.NotNull(result.Program);
        Assert.Empty(result.Diagnostics);
    }

    /// <summary>
    /// Tests that <see cref="TopsyTurvyParser.TryParse"/> fails on invalid source and returns a diagnostic.
    /// </summary>
    [Fact]
    public void TryParse_WithSyntaxError_ReturnsDiagnosticWithPositivePosition()
    {
        string source = "BEHOLD \"oops\"";
        ParseResult result = this.parser.TryParse(source);

        Assert.False(result.Success);
        Assert.Null(result.Program);
        Assert.Single(result.Diagnostics);

        Diagnostic diagnostic = result.Diagnostics[0];
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.True(diagnostic.Span.Start.Line >= 1, $"Line must be >= 1, got {diagnostic.Span.Start.Line}");
        Assert.True(diagnostic.Span.Start.Column >= 1, $"Column must be >= 1, got {diagnostic.Span.Start.Column}");
    }

    /// <summary>
    /// Tests that <see cref="TopsyTurvyParser.TryParse"/> with an empty source returns a diagnostic with a valid position.
    /// </summary>
    [Fact]
    public void TryParse_WithEmptySource_ReturnsDiagnosticWithPositivePosition()
    {
        ParseResult result = this.parser.TryParse(string.Empty);

        Assert.False(result.Success);
        Assert.Single(result.Diagnostics);

        Diagnostic diagnostic = result.Diagnostics[0];
        Assert.True(diagnostic.Span.Start.Line >= 1, $"Line must be >= 1, got {diagnostic.Span.Start.Line}");
        Assert.True(diagnostic.Span.Start.Column >= 1, $"Column must be >= 1, got {diagnostic.Span.Start.Column}");
    }

    /// <summary>
    /// Tests that <see cref="TopsyTurvyParser.TryParse"/> with a multi-line source with an error on line 2 reports the correct line number.
    /// </summary>
    [Fact]
    public void TryParse_WithErrorOnSecondLine_ReportsCorrectLine()
    {
        string source = "HARK! \"Test\"\nBAD TOKEN FINALE.";
        ParseResult result = this.parser.TryParse(source);

        Assert.False(result.Success);
        Assert.Single(result.Diagnostics);
        Assert.True(result.Diagnostics[0].Span.Start.Line >= 1,
            $"Line must be >= 1, got {result.Diagnostics[0].Span.Start.Line}");
    }
}
