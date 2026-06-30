using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Parser;

namespace BWHazel.TopsyTurvy.Tests.Parser;

/// <summary>
/// High-level integration tests for <see cref="TopsyTurvyParser"/>.
/// </summary>
public class TopsyTurvyParserTests
{
    private readonly TopsyTurvyParser parser = new();

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyParser.Parse"/> method correctly parses a minimal program.
    /// </summary>
    [Theory]
    [InlineData("HARK! \"Hello\" FINALE.", "Hello")]
    public void Parse_WithMinimalProgram_ReturnsTitle(string source, string expectedTitle)
    {
        ProgramNode program = this.parser.Parse(source);
        program.Title.ShouldBe(expectedTitle);
    }

    /// <summary>
    /// Tests the <see cref="TopsyTurvyParser.Parse"/> method correctly parses a minimal program with a subtitle.
    /// </summary>
    [Fact]
    public void Parse_WithMinimalProgramWithSubtitle_ReturnsSubtitle()
    {
        string source = "HARK! \"Title\" or, \"Subtitle\" FINALE.";
        ProgramNode program = this.parser.Parse(source);
        program.Subtitle.ShouldBe("Subtitle");
    }

    /// <summary>
    /// Tests that the <see cref="TopsyTurvyParser.Parse"/> method correctly parses an assignment statement.
    /// </summary>
    [Fact]
    public void Parse_WithProgramWithAssignment_ReturnsAssignment()
    {
        string source = "HARK! \"Title\" Ko-Ko IS APPOINTED 42 FINALE.";
        ProgramNode program = this.parser.Parse(source);

        program.Statements.ShouldHaveSingleItem();
        Statement statement = program.Statements[0];
        statement.ShouldBeOfType<AssignmentNode>();
        AssignmentNode assignment = (AssignmentNode)statement;
        assignment.Target.ShouldBe("Ko-Ko");
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
        expression.Operator.ShouldBe(Operator.WovenOf);
        expression.Arguments.Count.ShouldBe(3);
    }

    /// <summary>
    /// Tests that <see cref="TopsyTurvyParser.TryParse"/> succeeds on valid source and returns no diagnostics.
    /// </summary>
    [Fact]
    public void TryParse_WithValidSource_ReturnsSuccessAndNoDiagnostics()
    {
        string source = "HARK! \"Test\" BEHOLD \"hello\" FINALE.";
        ParseResult result = this.parser.TryParse(source);

        result.Success.ShouldBeTrue();
        result.Program.ShouldNotBeNull();
        result.Diagnostics.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that <see cref="TopsyTurvyParser.TryParse"/> fails on invalid source and returns a diagnostic.
    /// </summary>
    [Fact]
    public void TryParse_WithSyntaxError_ReturnsDiagnosticWithPositivePosition()
    {
        string source = "BEHOLD \"oops\"";
        ParseResult result = this.parser.TryParse(source);

        result.Success.ShouldBeFalse();
        result.Program.ShouldBeNull();
        result.Diagnostics.ShouldHaveSingleItem();

        Diagnostic diagnostic = result.Diagnostics[0];
        diagnostic.Severity.ShouldBe(DiagnosticSeverity.Error);
        (diagnostic.Span.Start.Line >= 1).ShouldBeTrue($"Line must be >= 1, got {diagnostic.Span.Start.Line}");
        (diagnostic.Span.Start.Column >= 1).ShouldBeTrue($"Column must be >= 1, got {diagnostic.Span.Start.Column}");
    }

    /// <summary>
    /// Tests that <see cref="TopsyTurvyParser.TryParse"/> with an empty source returns a diagnostic with a valid position.
    /// </summary>
    [Fact]
    public void TryParse_WithEmptySource_ReturnsDiagnosticWithPositivePosition()
    {
        ParseResult result = this.parser.TryParse(string.Empty);

        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldHaveSingleItem();

        Diagnostic diagnostic = result.Diagnostics[0];
        (diagnostic.Span.Start.Line >= 1).ShouldBeTrue($"Line must be >= 1, got {diagnostic.Span.Start.Line}");
        (diagnostic.Span.Start.Column >= 1).ShouldBeTrue($"Column must be >= 1, got {diagnostic.Span.Start.Column}");
    }

    /// <summary>
    /// Tests that <see cref="TopsyTurvyParser.TryParse"/> with a multi-line source with an error on line 2 reports the correct line number.
    /// </summary>
    [Fact]
    public void TryParse_WithErrorOnSecondLine_ReportsCorrectLine()
    {
        string source = "HARK! \"Test\"\nBAD TOKEN FINALE.";
        ParseResult result = this.parser.TryParse(source);

        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldHaveSingleItem();
        (result.Diagnostics[0].Span.Start.Line >= 1).ShouldBeTrue($"Line must be >= 1, got {result.Diagnostics[0].Span.Start.Line}");
    }

    /// <summary>
    /// Tests that <see cref="TopsyTurvyParser.TryParse"/> with a reserved word used as a variable name returns a non-null program with a diagnostic.
    /// </summary>
    [Fact]
    public void TryParse_WithReservedWordAsVariableName_ReturnsNonNullProgramWithDiagnostic()
    {
        string source = "HARK! \"T\" PRAY WELCOME BOTH AS A PEER FINALE.";
        ParseResult result = this.parser.TryParse(source);

        result.Program.ShouldNotBeNull();
        result.Diagnostics.ShouldNotBeEmpty();
    }

    /// <summary>
    /// Tests that <see cref="TopsyTurvyParser.Parse"/> with a reserved word used as a variable name throws a <see cref="TopsyTurvySyntaxException"/>.
    /// </summary>
    [Fact]
    public void Parse_WithReservedWordAsVariableName_ThrowsSyntaxException()
    {
        string source = "HARK! \"T\" PRAY WELCOME BOTH AS A PEER FINALE.";
        Should.Throw<TopsyTurvySyntaxException>(() => this.parser.Parse(source));
    }

    /// <summary>
    /// Tests that <see cref="TopsyTurvyParser.Parse"/> with a reserved word used as a function name throws a <see cref="TopsyTurvySyntaxException"/>.
    /// </summary>
    [Fact]
    public void Parse_WithReservedWordAsFunctionName_ThrowsSyntaxException()
    {
        string source = "HARK! \"T\" IT IS MY DUTY TO PERFORM DUTY UNDER NO OBLIGATION MY DUTY IS DISCHARGED. FINALE.";
        Should.Throw<TopsyTurvySyntaxException>(() => this.parser.Parse(source));
    }

    /// <summary>
    /// Tests that <see cref="TopsyTurvyParser.Parse"/> with a reserved word used as a parameter name throws a <see cref="TopsyTurvySyntaxException"/>.
    /// </summary>
    [Fact]
    public void Parse_WithReservedWordAsParameterName_ThrowsSyntaxException()
    {
        string source = "HARK! \"T\" IT IS MY DUTY TO PERFORM greet UNDER THE TERMS OF ALL MY DUTY IS DISCHARGED. FINALE.";
        Should.Throw<TopsyTurvySyntaxException>(() => this.parser.Parse(source));
    }

    /// <summary>
    /// Tests that <see cref="TopsyTurvyParser.TryParse"/> reports a syntax error on the correct line when a
    /// top-level statement partially matches (opening keyword consumed, body invalid).
    /// </summary>
    [Fact]
    public void TryParse_WithSyntaxErrorInTopLevelStatement_DiagnosticIsOnErrorLine()
    {
        string source = "HARK! \"Test\"\nPRAY WELCOME x AS A WrongType\nFINALE.";

        ParseResult result = this.parser.TryParse(source);

        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldHaveSingleItem();
        result.Diagnostics[0].Span.Start.Line.ShouldBe(2);
    }

    /// <summary>
    /// Tests that <see cref="TopsyTurvyParser.TryParse"/> reports a syntax error on the correct line when
    /// a statement inside a function body is invalid, not on the function definition line.
    /// </summary>
    [Fact]
    public void TryParse_WithSyntaxErrorInsideFunctionBody_DiagnosticIsOnErrorLine()
    {
        string source = "HARK! \"Test\"\nIT IS MY DUTY TO PERFORM greet UNDER NO OBLIGATION\nPRAY WELCOME x AS A WrongType\nMY DUTY IS DISCHARGED.\nFINALE.";

        ParseResult result = this.parser.TryParse(source);

        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldHaveSingleItem();
        result.Diagnostics[0].Span.Start.Line.ShouldBe(3);
    }

    /// <summary>
    /// Tests that <see cref="TopsyTurvyParser.TryParse"/> reports a syntax error on the correct line when
    /// a statement inside a conditional true-block is invalid, not on the conditional opening line.
    /// </summary>
    [Fact]
    public void TryParse_WithSyntaxErrorInsideConditionalBlock_DiagnosticIsOnErrorLine()
    {
        string source = "HARK! \"Test\"\nSHOULD IT TRANSPIRE THAT VERITY\nQUITE SO.\nPRAY WELCOME x AS A WrongType\nSO MUCH FOR THAT.\nFINALE.";
        
        ParseResult result = this.parser.TryParse(source);

        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldHaveSingleItem();
        result.Diagnostics[0].Span.Start.Line.ShouldBe(4);
    }
}
