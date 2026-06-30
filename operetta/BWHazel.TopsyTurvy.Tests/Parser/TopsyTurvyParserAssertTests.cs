using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Parser;

namespace BWHazel.TopsyTurvy.Tests.Parser;

/// <summary>
/// Tests for assert statement forms parsed by the <see cref="TopsyTurvyParser"/> class.
/// </summary>
public class TopsyTurvyParserAssertTests
{
    private readonly TopsyTurvyParser parser = new();

    /// <summary>
    /// Tests that an assert statement produces an <see cref="AssertNode"/> with <see cref="AssertNode.Condition"/> and <see cref="AssertNode.ErrorMessage"/> set correctly.
    /// </summary>
    [Fact]
    public void Parse_AssertStatement_ParsesConditionAndErrorMessage()
    {
        AssertNode node = this.ParseFirstStatement<AssertNode>(
            """THE LAW IS VERITY THAT "Assertion failed" """);

        node.Condition.ShouldBeOfType<LiteralNode>().Value.ShouldBe(true);
        LiteralNode message = node.ErrorMessage.ShouldBeOfType<LiteralNode>();
        message.Value.ShouldBe("Assertion failed");
        message.Type.ShouldBe(LiteralType.String);
    }

    /// <summary>
    /// Tests that an assert statement with a comparison condition produces an <see cref="AssertNode"/> with a <see cref="PrefixExpressionNode"/> condition.
    /// </summary>
    [Fact]
    public void Parse_AssertStatement_WithComparisonCondition_ParsesPrefixExpressionNode()
    {
        AssertNode node = this.ParseFirstStatement<AssertNode>(
            """THE LAW IS PRE-ADAMITE score AND 0 THAT "Score must be positive" """);

        PrefixExpressionNode condition = node.Condition.ShouldBeOfType<PrefixExpressionNode>();
        condition.Operator.ShouldBe(Operator.PreAdamite);
        condition.Arguments.Count.ShouldBe(2);
    }

    /// <summary>
    /// Tests that an assert statement with an identifier error message produces an <see cref="AssertNode"/> with an <see cref="IdentifierNode"/> error message.
    /// </summary>
    [Fact]
    public void Parse_AssertStatement_WithIdentifierErrorMessage_ParsesIdentifierNode()
    {
        AssertNode node = this.ParseFirstStatement<AssertNode>(
            "THE LAW IS VERITY THAT errorMessage");

        node.ErrorMessage.ShouldBeOfType<IdentifierNode>().Name.ShouldBe("errorMessage");
    }

    /// <summary>
    /// Tests that an assert statement with a falsy boolean literal condition parses correctly.
    /// </summary>
    [Fact]
    public void Parse_AssertStatement_WithFalsyCondition_ParsesLiteralNode()
    {
        AssertNode node = this.ParseFirstStatement<AssertNode>(
            """THE LAW IS NAY THAT "Never true" """);

        LiteralNode condition = node.Condition.ShouldBeOfType<LiteralNode>();
        condition.Value.ShouldBe(false);
        condition.Type.ShouldBe(LiteralType.Boolean);
    }

    /// <summary>
    /// Parses a single statement of a specific type from a source string.
    /// </summary>
    /// <typeparam name="T">The type of statement to parse.</typeparam>
    /// <param name="statementSource">The source string containing the statement.</param>
    private T ParseFirstStatement<T>(string statementSource) where T : Statement
    {
        ProgramNode program = this.parser.Parse($"HARK! \"T\" {statementSource} FINALE.");
        Statement statement = program.Statements.ShouldHaveSingleItem();
        return statement.ShouldBeOfType<T>();
    }
}
