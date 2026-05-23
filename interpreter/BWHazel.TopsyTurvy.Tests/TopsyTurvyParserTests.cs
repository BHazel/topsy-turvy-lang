using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Parser;
using Xunit;

namespace BWHazel.TopsyTurvy.Tests;

/// <summary>
/// Tests for <see cref="TopsyTurvyParser"/> class.
/// </summary>
public class ParserTests
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
}
