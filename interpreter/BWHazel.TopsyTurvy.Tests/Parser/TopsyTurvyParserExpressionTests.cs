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

        Assert.Equal(LiteralType.Integer, node.Type);
        Assert.Equal(42, node.Value);
    }

    /// <summary>
    /// Tests that a floating-point literal produces a <see cref="LiteralNode"/> with type <see cref="LiteralType.Float"/> and the correct value.
    /// </summary>
    [Fact]
    public void Parse_WithFloatLiteral_ProducesLiteralNodeWithCorrectValue()
    {
        LiteralNode node = this.ParsePrintExpression<LiteralNode>("3.14");

        Assert.Equal(LiteralType.Float, node.Type);
        Assert.Equal(3.14, (double)node.Value!);
    }

    /// <summary>
    /// Tests that a string literal produces a <see cref="LiteralNode"/> with type <see cref="LiteralType.String"/> and the correct value.
    /// </summary>
    [Fact]
    public void Parse_WithStringLiteral_ProducesLiteralNodeWithCorrectValue()
    {
        LiteralNode node = this.ParsePrintExpression<LiteralNode>("\"hello\"");

        Assert.Equal(LiteralType.String, node.Type);
        Assert.Equal("hello", node.Value);
    }

    /// <summary>
    /// Tests that VERITY produces a <see cref="LiteralNode"/> with type <see cref="LiteralType.Boolean"/> and value of <c>true</c>.
    /// </summary>
    [Fact]
    public void Parse_WithVerityLiteral_ProducesTrueBooleanLiteralNode()
    {
        LiteralNode node = this.ParsePrintExpression<LiteralNode>("VERITY");

        Assert.Equal(LiteralType.Boolean, node.Type);
        Assert.Equal(true, node.Value);
    }

    /// <summary>
    /// Tests that NAY produces a <see cref="LiteralNode"/> with type <see cref="LiteralType.Boolean"/> and value of <c>false</c>.
    /// </summary>
    [Fact]
    public void Parse_WithNayLiteral_ProducesFalseBooleanLiteralNode()
    {
        LiteralNode node = this.ParsePrintExpression<LiteralNode>("NAY");

        Assert.Equal(LiteralType.Boolean, node.Type);
        Assert.Equal(false, node.Value);
    }

    /// <summary>
    /// Tests that NAUGHT produces a <see cref="LiteralNode"/> with type <see cref="LiteralType.Null"/> and a null value.
    /// </summary>
    [Fact]
    public void Parse_WithNaughtLiteral_ProducesNullLiteralNode()
    {
        LiteralNode node = this.ParsePrintExpression<LiteralNode>("NAUGHT");

        Assert.Equal(LiteralType.Null, node.Type);
        Assert.Null(node.Value);
    }

    /// <summary>
    /// Tests that a string literal with a tilde-n escape sequence expands to a new-line character.
    /// </summary>
    [Fact]
    public void Parse_WithStringLiteralWithNewlineEscape_ExpandsEscapeSequence()
    {
        LiteralNode node = this.ParsePrintExpression<LiteralNode>("\"line1~nline2\"");

        Assert.Equal("line1\nline2", node.Value);
    }

    /// <summary>
    /// Tests that a string literal with a tilde-quote escape sequence expands to a double-quote character.
    /// </summary>
    [Fact]
    public void Parse_WithStringLiteralWithQuoteEscape_ExpandsEscapeSequence()
    {
        LiteralNode node = this.ParsePrintExpression<LiteralNode>("\"say ~\"hello~\"\"");

        Assert.Equal("say \"hello\"", node.Value);
    }

    /// <summary>
    /// Tests that JUST SO as a standalone expression produces an <see cref="IdentifierNode"/> named JUST SO.
    /// </summary>
    [Fact]
    public void Parse_WithJustSo_ProducesIdentifierNodeNamedJustSo()
    {
        ProgramNode program = this.parser.Parse("HARK! \"T\" JUST SO FINALE.");
        ExpressionStatement stmt = Assert.IsType<ExpressionStatement>(Assert.Single(program.Statements));
        IdentifierNode node = Assert.IsType<IdentifierNode>(stmt.Expression);

        Assert.Equal("JUST SO", node.Name);
    }

    /// <summary>
    /// Tests that a bare identifier on the right-hand side of an assignment produces an <see cref="IdentifierNode"/> with the correct name.
    /// </summary>
    [Fact]
    public void Parse_WithIdentifier_ProducesIdentifierNodeWithCorrectName()
    {
        ProgramNode program = this.parser.Parse("HARK! \"T\" x IS APPOINTED greeting FINALE.");
        AssignmentNode assign = Assert.IsType<AssignmentNode>(Assert.Single(program.Statements));
        IdentifierNode node = Assert.IsType<IdentifierNode>(assign.Value);

        Assert.Equal("greeting", node.Name);
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
    public void Parse_WithBinaryPrefixOperator_ProducesCorrectOperatorWithTwoArguments(
        string expressionSource,
        Operator expectedOperator)
    {
        PrefixExpressionNode node = this.ParsePrintExpression<PrefixExpressionNode>(expressionSource);

        Assert.Equal(expectedOperator, node.Operator);
        Assert.Equal(2, node.Arguments.Count);
    }

    /// <summary>
    /// Tests that HARDLY EVER produces a <see cref="PrefixExpressionNode"/> with operator <see cref="Operator.HardlyEver"/> and one argument.
    /// </summary>
    [Fact]
    public void Parse_WithHardlyEver_ProducesUnaryOperatorWithOneArgument()
    {
        PrefixExpressionNode node = this.ParsePrintExpression<PrefixExpressionNode>("HARDLY EVER VERITY");

        Assert.Equal(Operator.HardlyEver, node.Operator);
        Assert.Single(node.Arguments);
    }
    
    /// <summary>
    /// Tests that WOVEN OF with three arguments produces a <see cref="PrefixExpressionNode"/> with three arguments.
    /// </summary>
    [Fact]
    public void Parse_WovenOf_WithThreeArguments_ProducesThreeArguments()
    {
        PrefixExpressionNode node = this.ParseExpressionStatement<PrefixExpressionNode>(
            "WOVEN OF \"a\" AND \"b\" AND \"c\" IF YOU PLEASE.");

        Assert.Equal(Operator.WovenOf, node.Operator);
        Assert.Equal(3, node.Arguments.Count);
    }

    /// <summary>
    /// Tests that ALL OF with two arguments produces a <see cref="PrefixExpressionNode"/> with operator <see cref="Operator.AllOf"/>.
    /// </summary>
    [Fact]
    public void Parse_AllOf_WithTwoArguments_ProducesTwoArguments()
    {
        PrefixExpressionNode node = this.ParseExpressionStatement<PrefixExpressionNode>(
            "ALL OF VERITY AND NAY IF YOU PLEASE.");

        Assert.Equal(Operator.AllOf, node.Operator);
        Assert.Equal(2, node.Arguments.Count);
    }

    /// <summary>
    /// Tests that ANY OF with two arguments produces a <see cref="PrefixExpressionNode"/> with operator <see cref="Operator.AnyOf"/>.
    /// </summary>
    [Fact]
    public void Parse_AnyOf_WithTwoArguments_ProducesTwoArguments()
    {
        PrefixExpressionNode node = this.ParseExpressionStatement<PrefixExpressionNode>(
            "ANY OF VERITY AND NAY IF YOU PLEASE.");

        Assert.Equal(Operator.AnyOf, node.Operator);
        Assert.Equal(2, node.Arguments.Count);
    }

    /// <summary>
    /// Tests that SUMMON … WITH NOTHING produces a <see cref="PrefixExpressionNode"/> with operator <see cref="Operator.Summon"/> whose only argument is the function name identifier.
    /// </summary>
    [Fact]
    public void Parse_Summon_WithNothing_ProducesOneArgumentContainingFunctionName()
    {
        PrefixExpressionNode node = this.ParseExpressionStatement<PrefixExpressionNode>(
            "SUMMON greet WITH NOTHING IF YOU PLEASE.");

        Assert.Equal(Operator.Summon, node.Operator);
        Assert.Single(node.Arguments);
        IdentifierNode nameArg = Assert.IsType<IdentifierNode>(node.Arguments[0]);
        Assert.Equal("greet", nameArg.Name);
    }

    /// <summary>
    /// Tests that SUMMON … WITH arguments prepends the function name and includes all supplied arguments.
    /// </summary>
    [Fact]
    public void Parse_Summon_WithArguments_PrependsFunctionNameAndIncludesArgs()
    {
        PrefixExpressionNode node = this.ParseExpressionStatement<PrefixExpressionNode>(
            "SUMMON add WITH 3 AND 4 IF YOU PLEASE.");

        Assert.Equal(Operator.Summon, node.Operator);
        Assert.Equal(3, node.Arguments.Count);
        IdentifierNode nameArg = Assert.IsType<IdentifierNode>(node.Arguments[0]);
        Assert.Equal("add", nameArg.Name);
    }

    /// <summary>
    /// Tests that a nested arithmetic expression is parsed correctly preserving argument structure.
    /// </summary>
    [Fact]
    public void Parse_NestedArithmetic_PreservesArgumentStructure()
    {
        PrefixExpressionNode outerExpression = this.ParsePrintExpression<PrefixExpressionNode>(
            "SUM OF PRODUCT OF 2 AND 3 AND 4");

        Assert.Equal(Operator.Sum, outerExpression.Operator);
        Assert.Equal(2, outerExpression.Arguments.Count);
        PrefixExpressionNode innerExpression = Assert.IsType<PrefixExpressionNode>(outerExpression.Arguments[0]);
        Assert.Equal(Operator.Product, innerExpression.Operator);
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
        PrintNode print = Assert.IsType<PrintNode>(Assert.Single(program.Statements));
        return Assert.IsType<T>(print.Expression);
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
        ExpressionStatement stmt = Assert.IsType<ExpressionStatement>(Assert.Single(program.Statements));
        return Assert.IsType<T>(stmt.Expression);
    }
}
