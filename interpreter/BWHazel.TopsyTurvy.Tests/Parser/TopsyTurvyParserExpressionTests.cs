using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Parser;

namespace BWHazel.TopsyTurvy.Tests.Parser;

/// <summary>
/// Tests for every expression form parsed by the <see cref="TopsyTurvyParser"/> class.
/// </summary>
public class TopsyTurvyParserExpressionTests
{
    private readonly TopsyTurvyParser parser = new();

    /// <summary>
    /// Tests that an integer literal produces a <see cref="LiteralNode"/> with type <see cref="LiteralType.Integer"/> and the correct value.
    /// </summary>
    [Fact]
    public void Parse_WithIntegerLiteral_ProducesLiteralNodeWithCorrectValue()
    {
        LiteralNode node = this.ParsePrintExpression<LiteralNode>("42");

        node.Type.ShouldBe(LiteralType.Integer);
        node.Value.ShouldBe(42);
    }

    /// <summary>
    /// Tests that a floating-point literal produces a <see cref="LiteralNode"/> with type <see cref="LiteralType.Double"/> and the correct value.
    /// </summary>
    [Fact]
    public void Parse_WithFloatLiteral_ProducesLiteralNodeWithCorrectValue()
    {
        LiteralNode node = this.ParsePrintExpression<LiteralNode>("3.14");

        node.Type.ShouldBe(LiteralType.Double);
        ((double)node.Value!).ShouldBe(3.14, tolerance: 1e-10);
    }

    /// <summary>
    /// Tests that a string literal produces a <see cref="LiteralNode"/> with type <see cref="LiteralType.String"/> and the correct value.
    /// </summary>
    [Fact]
    public void Parse_WithStringLiteral_ProducesLiteralNodeWithCorrectValue()
    {
        LiteralNode node = this.ParsePrintExpression<LiteralNode>("\"hello\"");

        node.Type.ShouldBe(LiteralType.String);
        node.Value.ShouldBe("hello");
    }

    /// <summary>
    /// Tests that VERITY produces a <see cref="LiteralNode"/> with type <see cref="LiteralType.Boolean"/> and value of <c>true</c>.
    /// </summary>
    [Fact]
    public void Parse_WithVerityLiteral_ProducesTrueBooleanLiteralNode()
    {
        LiteralNode node = this.ParsePrintExpression<LiteralNode>("VERITY");

        node.Type.ShouldBe(LiteralType.Boolean);
        node.Value.ShouldBe(true);
    }

    /// <summary>
    /// Tests that NAY produces a <see cref="LiteralNode"/> with type <see cref="LiteralType.Boolean"/> and value of <c>false</c>.
    /// </summary>
    [Fact]
    public void Parse_WithNayLiteral_ProducesFalseBooleanLiteralNode()
    {
        LiteralNode node = this.ParsePrintExpression<LiteralNode>("NAY");

        node.Type.ShouldBe(LiteralType.Boolean);
        node.Value.ShouldBe(false);
    }

    /// <summary>
    /// Tests that NAUGHT produces a <see cref="LiteralNode"/> with type <see cref="LiteralType.Null"/> and a null value.
    /// </summary>
    [Fact]
    public void Parse_WithNaughtLiteral_ProducesNullLiteralNode()
    {
        LiteralNode node = this.ParsePrintExpression<LiteralNode>("NAUGHT");

        node.Type.ShouldBe(LiteralType.Null);
        node.Value.ShouldBeNull();
    }

    /// <summary>
    /// Tests that a string literal with a tilde-n escape sequence expands to a new-line character.
    /// </summary>
    [Fact]
    public void Parse_WithStringLiteralWithNewlineEscape_ExpandsEscapeSequence()
    {
        LiteralNode node = this.ParsePrintExpression<LiteralNode>("\"line1~nline2\"");

        node.Value.ShouldBe("line1\nline2");
    }

    /// <summary>
    /// Tests that a string literal with a tilde-quote escape sequence expands to a double-quote character.
    /// </summary>
    [Fact]
    public void Parse_WithStringLiteralWithQuoteEscape_ExpandsEscapeSequence()
    {
        LiteralNode node = this.ParsePrintExpression<LiteralNode>("\"say ~\"hello~\"\"");

        node.Value.ShouldBe("say \"hello\"");
    }

    /// <summary>
    /// Tests that JUST SO as a standalone expression produces an <see cref="IdentifierNode"/> named JUST SO.
    /// </summary>
    [Fact]
    public void Parse_WithJustSo_ProducesIdentifierNodeNamedJustSo()
    {
        ProgramNode program = this.parser.Parse("HARK! \"T\" JUST SO FINALE.");
        ExpressionStatement statement = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<ExpressionStatement>();
        IdentifierNode node = statement.Expression.ShouldBeOfType<IdentifierNode>();

        node.Name.ShouldBe("JUST SO");
    }

    /// <summary>
    /// Tests that a bare identifier on the right-hand side of an assignment produces an <see cref="IdentifierNode"/> with the correct name.
    /// </summary>
    [Fact]
    public void Parse_WithIdentifier_ProducesIdentifierNodeWithCorrectName()
    {
        ProgramNode program = this.parser.Parse("HARK! \"T\" x IS APPOINTED greeting FINALE.");
        AssignmentNode assign = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<AssignmentNode>();
        IdentifierNode node = assign.Value.ShouldBeOfType<IdentifierNode>();

        node.Name.ShouldBe("greeting");
    }

    /// <summary>
    /// Tests that each binary prefix operator produces the correct <see cref="Operator"/> value with two arguments.
    /// </summary>
    /// <param name="expressionSource">The source code of the expression to parse.</param>
    /// <param name="expectedOperator">The expected operator enum value.</param>
    [Theory]
    [InlineData("SUM OF 1 AND 2",           Operator.Sum)]
    [InlineData("DIFFERENCE OF 5 AND 3",    Operator.Difference)]
    [InlineData("PRODUCT OF 3 AND 4",       Operator.Product)]
    [InlineData("QUOTIENT OF 10 AND 2",     Operator.Quotient)]
    [InlineData("REMAINDER OF 7 AND 3",     Operator.Remainder)]
    [InlineData("LARGER OF 8 AND 3",        Operator.Larger)]
    [InlineData("SMALLER OF 8 AND 3",       Operator.Smaller)]
    [InlineData("BOTH VERITY AND NAY",      Operator.Both)]
    [InlineData("EITHER NAY AND VERITY",    Operator.Either)]
    [InlineData("ALIKE 1 AND 1",            Operator.Alike)]
    [InlineData("UNLIKE 1 AND 2",           Operator.Unlike)]
    [InlineData("PRE-ADAMITE 5 AND 3",      Operator.PreAdamite)]
    [InlineData("LOWER DEGREE 3 AND 5",     Operator.LowerDegree)]
    [InlineData("CHORD OF 5 AND 3",         Operator.ChordOf)]
    [InlineData("HARMONY OF 5 AND 3",       Operator.HarmonyOf)]
    [InlineData("DISCORD OF 5 AND 3",       Operator.DiscordOf)]
    public void Parse_WithBinaryPrefixOperator_ProducesCorrectOperatorWithTwoArguments(
        string expressionSource,
        Operator expectedOperator)
    {
        PrefixExpressionNode node = this.ParsePrintExpression<PrefixExpressionNode>(expressionSource);

        node.Operator.ShouldBe(expectedOperator);
        node.Arguments.Count.ShouldBe(2);
    }

    /// <summary>
    /// Tests that each unary prefix operator produces a <see cref="PrefixExpressionNode"/> with the correct
    /// <see cref="Operator"/> value and one argument.
    /// </summary>
    /// <param name="expressionSource">The source code of the expression to parse.</param>
    /// <param name="expectedOperator">The expected operator enum value.</param>
    [Theory]
    [InlineData("HARDLY EVER VERITY",   Operator.HardlyEver)]
    [InlineData("INVERSION OF 5",       Operator.InversionOf)]
    [InlineData("TRANSPOSITION UP 4",   Operator.TranspositionUp)]
    [InlineData("TRANSPOSITION DOWN 8", Operator.TranspositionDown)]
    public void Parse_WithUnaryPrefixOperator_ProducesCorrectOperatorWithOneArgument(string expressionSource, Operator expectedOperator)
    {
        PrefixExpressionNode node = this.ParsePrintExpression<PrefixExpressionNode>(expressionSource);

        node.Operator.ShouldBe(expectedOperator);
        node.Arguments.ShouldHaveSingleItem();
    }

    /// <summary>
    /// Tests that WOVEN OF with three arguments produces a <see cref="PrefixExpressionNode"/> with three arguments.
    /// </summary>
    [Fact]
    public void Parse_WovenOf_WithThreeArguments_ProducesThreeArguments()
    {
        PrefixExpressionNode node = this.ParseExpressionStatement<PrefixExpressionNode>(
            "WOVEN OF \"a\" AND \"b\" AND \"c\" IF YOU PLEASE.");

        node.Operator.ShouldBe(Operator.WovenOf);
        node.Arguments.Count.ShouldBe(3);
    }

    /// <summary>
    /// Tests that ALL OF with two arguments produces a <see cref="PrefixExpressionNode"/> with operator <see cref="Operator.AllOf"/>.
    /// </summary>
    [Fact]
    public void Parse_AllOf_WithTwoArguments_ProducesTwoArguments()
    {
        PrefixExpressionNode node = this.ParseExpressionStatement<PrefixExpressionNode>(
            "ALL OF VERITY AND NAY IF YOU PLEASE.");

        node.Operator.ShouldBe(Operator.AllOf);
        node.Arguments.Count.ShouldBe(2);
    }

    /// <summary>
    /// Tests that ANY OF with two arguments produces a <see cref="PrefixExpressionNode"/> with operator <see cref="Operator.AnyOf"/>.
    /// </summary>
    [Fact]
    public void Parse_AnyOf_WithTwoArguments_ProducesTwoArguments()
    {
        PrefixExpressionNode node = this.ParseExpressionStatement<PrefixExpressionNode>(
            "ANY OF VERITY AND NAY IF YOU PLEASE.");

        node.Operator.ShouldBe(Operator.AnyOf);
        node.Arguments.Count.ShouldBe(2);
    }

    /// <summary>
    /// Tests that SUMMON … WITH NOTHING produces a <see cref="PrefixExpressionNode"/> with operator <see cref="Operator.Summon"/> whose only argument is the function name identifier.
    /// </summary>
    [Fact]
    public void Parse_Summon_WithNothing_ProducesOneArgumentContainingFunctionName()
    {
        PrefixExpressionNode node = this.ParseExpressionStatement<PrefixExpressionNode>(
            "SUMMON greet WITH NOTHING IF YOU PLEASE.");

        node.Operator.ShouldBe(Operator.Summon);
        node.Arguments.ShouldHaveSingleItem();
        IdentifierNode nameArg = node.Arguments[0].ShouldBeOfType<IdentifierNode>();
        nameArg.Name.ShouldBe("greet");
    }

    /// <summary>
    /// Tests that SUMMON … WITH arguments prepends the function name and includes all supplied arguments.
    /// </summary>
    [Fact]
    public void Parse_Summon_WithArguments_PrependsFunctionNameAndIncludesArgs()
    {
        PrefixExpressionNode node = this.ParseExpressionStatement<PrefixExpressionNode>(
            "SUMMON add WITH 3 AND 4 IF YOU PLEASE.");

        node.Operator.ShouldBe(Operator.Summon);
        node.Arguments.Count.ShouldBe(3);
        IdentifierNode nameArg = node.Arguments[0].ShouldBeOfType<IdentifierNode>();
        nameArg.Name.ShouldBe("add");
    }

    /// <summary>
    /// Tests that a nested arithmetic expression is parsed correctly preserving argument structure.
    /// </summary>
    [Fact]
    public void Parse_NestedArithmetic_PreservesArgumentStructure()
    {
        PrefixExpressionNode outerExpression = this.ParsePrintExpression<PrefixExpressionNode>(
            "SUM OF PRODUCT OF 2 AND 3 AND 4");

        outerExpression.Operator.ShouldBe(Operator.Sum);
        outerExpression.Arguments.Count.ShouldBe(2);
        PrefixExpressionNode innerExpression = outerExpression.Arguments[0].ShouldBeOfType<PrefixExpressionNode>();
        innerExpression.Operator.ShouldBe(Operator.Product);
    }
    
    /// <summary>
    /// Tests that an integer literal parsed from a dedicated source line carries a real source span pointing to the
    /// literal token in the original source.
    /// </summary>
    [Fact]
    public void Parse_IntegerLiteral_SpanStartsAtCorrectLineAndColumn()
    {
        ProgramNode program = this.parser.Parse("HARK! \"Test\"\nBEHOLD 42\nFINALE.");

        PrintNode print = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<PrintNode>();
        LiteralNode node = print.Expression.ShouldBeOfType<LiteralNode>();
        node.Span.Start.Line.ShouldBe(2);
        (node.Span.Start.Column > 0).ShouldBeTrue();
    }

    /// <summary>
    /// Tests that an identifier expression parsed from a dedicated source line carries a real source span pointing to
    /// the identifier token.
    /// </summary>
    [Fact]
    public void Parse_IdentifierExpression_SpanStartsAtCorrectLineAndColumn()
    {
        ProgramNode program = this.parser.Parse("HARK! \"Test\"\nPRAY WELCOME x AS A PEER BEING 0\nBEHOLD x\nFINALE.");

        program.Statements.Count.ShouldBe(2);
        PrintNode print = program.Statements[1].ShouldBeOfType<PrintNode>();
        IdentifierNode node = print.Expression.ShouldBeOfType<IdentifierNode>();
        node.Span.Start.Line.ShouldBe(3);
        (node.Span.Start.Column > 0).ShouldBeTrue();
    }

    /// <summary>
    /// Tests that a string literal node carries a real source span at the correct position.
    /// </summary>
    [Fact]
    public void Parse_StringLiteral_SpanStartsAtCorrectLineAndColumn()
    {
        ProgramNode program = this.parser.Parse("HARK! \"Test\"\nBEHOLD \"hello\"\nFINALE.");

        PrintNode print = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<PrintNode>();
        LiteralNode node = print.Expression.ShouldBeOfType<LiteralNode>();
        node.Span.Start.Line.ShouldBe(2);
        node.Span.Start.Column.ShouldBe(8);
    }

    /// <summary>
    /// Tests that a boolean true literal node carries a real source span at the correct position.
    /// </summary>
    [Fact]
    public void Parse_BooleanTrueLiteral_SpanStartsAtCorrectLineAndColumn()
    {
        ProgramNode program = this.parser.Parse("HARK! \"Test\"\nBEHOLD VERITY\nFINALE.");

        PrintNode print = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<PrintNode>();
        LiteralNode node = print.Expression.ShouldBeOfType<LiteralNode>();
        node.Span.Start.Line.ShouldBe(2);
        node.Span.Start.Column.ShouldBe(8);
    }

    /// <summary>
    /// Tests that a boolean false literal node carries a real source span at the correct position.
    /// </summary>
    [Fact]
    public void Parse_BooleanFalseLiteral_SpanStartsAtCorrectLineAndColumn()
    {
        ProgramNode program = this.parser.Parse("HARK! \"Test\"\nBEHOLD NAY\nFINALE.");

        PrintNode print = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<PrintNode>();
        LiteralNode node = print.Expression.ShouldBeOfType<LiteralNode>();
        node.Span.Start.Line.ShouldBe(2);
        node.Span.Start.Column.ShouldBe(8);
    }

    /// <summary>
    /// Tests that a null literal node carries a real source span at the correct position.
    /// </summary>
    [Fact]
    public void Parse_NullLiteral_SpanStartsAtCorrectLineAndColumn()
    {
        ProgramNode program = this.parser.Parse("HARK! \"Test\"\nBEHOLD NAUGHT\nFINALE.");

        PrintNode print = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<PrintNode>();
        LiteralNode node = print.Expression.ShouldBeOfType<LiteralNode>();
        node.Span.Start.Line.ShouldBe(2);
        node.Span.Start.Column.ShouldBe(8);
    }

    /// <summary>
    /// Tests that a float literal node carries a real source span at the correct position.
    /// </summary>
    [Fact]
    public void Parse_FloatLiteral_SpanStartsAtCorrectLineAndColumn()
    {
        ProgramNode program = this.parser.Parse("HARK! \"Test\"\nBEHOLD 3.14\nFINALE.");

        PrintNode print = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<PrintNode>();
        LiteralNode node = print.Expression.ShouldBeOfType<LiteralNode>();
        node.Span.Start.Line.ShouldBe(2);
        node.Span.Start.Column.ShouldBe(8);
    }

    /// <summary>
    /// Tests that a character literal node carries a real source span at the correct position.
    /// </summary>
    [Fact]
    public void Parse_CharLiteral_SpanStartsAtCorrectLineAndColumn()
    {
        ProgramNode program = this.parser.Parse("HARK! \"Test\"\nBEHOLD 'A'\nFINALE.");

        PrintNode print = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<PrintNode>();
        LiteralNode node = print.Expression.ShouldBeOfType<LiteralNode>();
        node.Span.Start.Line.ShouldBe(2);
        node.Span.Start.Column.ShouldBe(8);
    }

    /// <summary>
    /// Tests that a prefix expression node carries a real source span starting at the operator keyword.
    /// </summary>
    [Fact]
    public void Parse_PrefixExpression_SpanStartsAtCorrectLineAndColumn()
    {
        ProgramNode program = this.parser.Parse("HARK! \"Test\"\nBEHOLD SUM OF 1 AND 2\nFINALE.");

        PrintNode print = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<PrintNode>();
        PrefixExpressionNode node = print.Expression.ShouldBeOfType<PrefixExpressionNode>();
        node.Span.Start.Line.ShouldBe(2);
        node.Span.Start.Column.ShouldBe(8);
    }

    /// <summary>
    /// Tests that the outer <see cref="PrefixExpressionNode"/> from a SUMMON call carries a span starting at the SUMMON keyword.
    /// </summary>
    [Fact]
    public void Parse_SummonExpression_OuterSpanStartsAtCorrectLineAndColumn()
    {
        ProgramNode program = this.parser.Parse("HARK! \"Test\"\nBEHOLD SUMMON greet WITH NOTHING IF YOU PLEASE.\nFINALE.");

        PrintNode print = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<PrintNode>();
        PrefixExpressionNode node = print.Expression.ShouldBeOfType<PrefixExpressionNode>();
        node.Span.Start.Line.ShouldBe(2);
        node.Span.Start.Column.ShouldBe(8);
    }

    /// <summary>
    /// Tests that the inner <see cref="IdentifierNode"/> for the function name in a SUMMON call carries a span
    /// pointing to the function name token, not the SUMMON keyword.
    /// </summary>
    [Fact]
    public void Parse_SummonExpression_FunctionNameIdentifierSpanIsAfterSummonKeyword()
    {
        ProgramNode program = this.parser.Parse("HARK! \"Test\"\nBEHOLD SUMMON greet WITH NOTHING IF YOU PLEASE.\nFINALE.");

        PrintNode print = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<PrintNode>();
        PrefixExpressionNode summon = print.Expression.ShouldBeOfType<PrefixExpressionNode>();
        IdentifierNode functionName = summon.Arguments[0].ShouldBeOfType<IdentifierNode>();
        functionName.Span.Start.Line.ShouldBe(2);
        functionName.Span.Start.Column.ShouldBe(15);
        (functionName.Span.Start.Column > summon.Span.Start.Column).ShouldBeTrue();
    }

    /// <summary>
    /// Tests that an array index expression carries a real source span starting at the VICTIM keyword.
    /// </summary>
    [Fact]
    public void Parse_ArrayIndexExpression_SpanStartsAtCorrectLineAndColumn()
    {
        ProgramNode program = this.parser.Parse("HARK! \"Test\"\nBEHOLD VICTIM 1 ON arr\nFINALE.");

        PrintNode print = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<PrintNode>();
        ArrayIndexNode node = print.Expression.ShouldBeOfType<ArrayIndexNode>();
        node.Span.Start.Line.ShouldBe(2);
        node.Span.Start.Column.ShouldBe(8);
    }

    /// <summary>
    /// Tests that an array length expression carries a real source span starting at the RECKONING OF keyword.
    /// </summary>
    [Fact]
    public void Parse_ArrayLengthExpression_SpanStartsAtCorrectLineAndColumn()
    {
        ProgramNode program = this.parser.Parse("HARK! \"Test\"\nBEHOLD RECKONING OF arr\nFINALE.");

        PrintNode print = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<PrintNode>();
        ArrayLengthNode node = print.Expression.ShouldBeOfType<ArrayLengthNode>();
        node.Span.Start.Line.ShouldBe(2);
        node.Span.Start.Column.ShouldBe(8);
    }

    /// <summary>
    /// Tests that an expression cast node carries a real source span starting at the AS IT WERE keyword.
    /// </summary>
    [Fact]
    public void Parse_ExpressionCast_SpanStartsAtCorrectLineAndColumn()
    {
        ProgramNode program = this.parser.Parse("HARK! \"Test\"\nBEHOLD AS IT WERE x AS A YARN\nFINALE.");

        PrintNode print = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<PrintNode>();
        ExpressionCastNode node = print.Expression.ShouldBeOfType<ExpressionCastNode>();
        node.Span.Start.Line.ShouldBe(2);
        node.Span.Start.Column.ShouldBe(8);
    }

    /// <summary>
    /// Tests that a ternary expression node carries a real source span starting at the true-value expression.
    /// </summary>
    [Fact]
    public void Parse_TernaryExpression_SpanStartsAtCorrectLineAndColumn()
    {
        ProgramNode program = this.parser.Parse("HARK! \"Test\"\nBEHOLD \"yes\" SHOULD IT TRANSPIRE THAT VERITY OTHERWISE, \"no\"\nFINALE.");

        PrintNode print = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<PrintNode>();
        TernaryExpressionNode node = print.Expression.ShouldBeOfType<TernaryExpressionNode>();
        node.Span.Start.Line.ShouldBe(2);
        node.Span.Start.Column.ShouldBe(8);
    }

    /// <summary>
    /// Tests that the implicit JUST SO expression carries a real source span at the correct position.
    /// </summary>
    [Fact]
    public void Parse_JustSoExpression_SpanStartsAtCorrectLineAndColumn()
    {
        ProgramNode program = this.parser.Parse("HARK! \"Test\"\nBEHOLD JUST SO\nFINALE.");

        PrintNode print = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<PrintNode>();
        IdentifierNode node = print.Expression.ShouldBeOfType<IdentifierNode>();
        node.Span.Start.Line.ShouldBe(2);
        node.Span.Start.Column.ShouldBe(8);
    }

    /// <summary>
    /// Tests that THE PROPS built-in array expression carries a real source span at the correct position.
    /// </summary>
    [Fact]
    public void Parse_ThePropsExpression_SpanStartsAtCorrectLineAndColumn()
    {
        ProgramNode program = this.parser.Parse("HARK! \"Test\"\nBEHOLD THE PROPS\nFINALE.");

        PrintNode print = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<PrintNode>();
        IdentifierNode node = print.Expression.ShouldBeOfType<IdentifierNode>();
        node.Span.Start.Line.ShouldBe(2);
        node.Span.Start.Column.ShouldBe(8);
    }

    /// <summary>
    /// Tests that the integer literal <c>42</c> on line 2 has a span end strictly after its start,
    /// confirming the span covers the token width and is not zero-length.
    /// </summary>
    [Fact]
    public void Parse_IntegerLiteral_SpanEndIsAfterStart()
    {
        ProgramNode program = this.parser.Parse("HARK! \"Test\"\nBEHOLD 42\nFINALE.");

        PrintNode print = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<PrintNode>();
        LiteralNode node = print.Expression.ShouldBeOfType<LiteralNode>();
        node.Span.End.Line.ShouldBe(2);
        node.Span.End.Column.ShouldBe(10);
    }

    /// <summary>
    /// Tests that when source contains an inline ASIDE: comment that is stripped by the pre-processor,
    /// the span of the expression on the following line still reflects the correct original line number.
    /// </summary>
    [Fact]
    public void Parse_WithInlineComment_SpanReflectsOriginalSourceLineNumber()
    {
        ProgramNode program = this.parser.Parse("HARK! \"Test\" ASIDE: inline comment\nBEHOLD 42\nFINALE.");

        PrintNode print = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<PrintNode>();
        LiteralNode node = print.Expression.ShouldBeOfType<LiteralNode>();
        node.Span.Start.Line.ShouldBe(2);
        node.Span.Start.Column.ShouldBe(8);
    }

    /// <summary>
    /// Parses an expression in a print statement and returns the expression node for testing.
    /// </summary>
    /// <typeparam name="T">The type of the expression node.</typeparam>
    /// <param name="expressionSource">The source code of the expression.</param>
    /// <returns>The parsed expression node of type <typeparamref name="T"/>.</returns>
    private T ParsePrintExpression<T>(string expressionSource) where T : Expression
    {
        ProgramNode program = this.parser.Parse($"HARK! \"T\" BEHOLD {expressionSource} FINALE.");
        PrintNode print = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<PrintNode>();
        return print.Expression.ShouldBeOfType<T>();
    }

    /// <summary>
    /// Parses an expression in a standalone statement and returns the expression node for testing.
    /// </summary>
    /// <typeparam name="T">The type of the expression node.</typeparam>
    /// <param name="expressionSource">The source code of the expression.</param>
    /// <returns>The parsed expression node of type <typeparamref name="T"/>.</returns>
    private T ParseExpressionStatement<T>(string expressionSource) where T : Expression
    {
        ProgramNode program = this.parser.Parse($"HARK! \"T\" {expressionSource} FINALE.");
        ExpressionStatement stmt = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<ExpressionStatement>();
        return stmt.Expression.ShouldBeOfType<T>();
    }
}
