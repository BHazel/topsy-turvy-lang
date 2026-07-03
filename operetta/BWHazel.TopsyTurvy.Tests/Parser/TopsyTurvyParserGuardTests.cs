using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Parser;

namespace BWHazel.TopsyTurvy.Tests.Parser;

/// <summary>
/// Tests for guard clause forms parsed by the <see cref="TopsyTurvyParser"/> class.
/// </summary>
public class TopsyTurvyParserGuardTests
{
    private readonly TopsyTurvyParser parser = new();

    /// <summary>
    /// Tests that a guard clause produces a <see cref="GuardNode"/> with a <see cref="GuardNode.Condition"/> and an empty <see cref="GuardNode.ElseBlock"/> when no body statements are given.
    /// </summary>
    [Fact]
    public void Parse_GuardClause_EmptyBody_ParsesConditionAndEmptyElseBlock()
    {
        GuardNode node = this.ParseFirstStatement<GuardNode>(
            "YEOMAN VERITY OTHERWISE, UNDER ORDERS.");

        node.Condition.ShouldBeOfType<LiteralNode>().Value.ShouldBe(true);
        node.ElseBlock.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that a guard clause with a boolean literal condition produces a <see cref="GuardNode"/> with the literal as the condition.
    /// </summary>
    [Fact]
    public void Parse_GuardClause_WithBooleanLiteralCondition_ParsesLiteralNode()
    {
        GuardNode node = this.ParseFirstStatement<GuardNode>(
            "YEOMAN NAY OTHERWISE, UNDER ORDERS.");

        LiteralNode condition = node.Condition.ShouldBeOfType<LiteralNode>();
        condition.Value.ShouldBe(false);
        condition.Type.ShouldBe(LiteralType.Boolean);
    }

    /// <summary>
    /// Tests that a guard clause with a comparison condition produces a <see cref="GuardNode"/> with a <see cref="PrefixExpressionNode"/> condition.
    /// </summary>
    [Fact]
    public void Parse_GuardClause_WithComparisonCondition_ParsesPrefixExpressionNode()
    {
        GuardNode node = this.ParseFirstStatement<GuardNode>(
            "YEOMAN PRE-ADAMITE score AND 0 OTHERWISE, UNDER ORDERS.");

        PrefixExpressionNode condition = node.Condition.ShouldBeOfType<PrefixExpressionNode>();
        condition.Operator.ShouldBe(Operator.PreAdamite);
        condition.Arguments.Count.ShouldBe(2);
    }

    /// <summary>
    /// Tests that a guard clause with a single body statement produces a <see cref="GuardNode"/> with one statement in <see cref="GuardNode.ElseBlock"/>.
    /// </summary>
    [Fact]
    public void Parse_GuardClause_WithSingleBodyStatement_ParsesOneElseBlockStatement()
    {
        GuardNode node = this.ParseFirstStatement<GuardNode>("""
            YEOMAN VERITY
            OTHERWISE,
              A HIDEOUS CURSE ON "Guard failed"
            UNDER ORDERS.
            """);

        node.ElseBlock.ShouldHaveSingleItem().ShouldBeOfType<ThrowNode>();
    }

    /// <summary>
    /// Tests that a guard clause with multiple body statements produces a <see cref="GuardNode"/> with all statements in <see cref="GuardNode.ElseBlock"/>.
    /// </summary>
    [Fact]
    public void Parse_GuardClause_WithMultipleBodyStatements_ParsesAllElseBlockStatements()
    {
        GuardNode node = this.ParseFirstStatement<GuardNode>("""
            YEOMAN VERITY
            OTHERWISE,
              BEHOLD "guard fired"
              A HIDEOUS CURSE ON "Guard failed"
            UNDER ORDERS.
            """);

        node.ElseBlock.Count.ShouldBe(2);
        node.ElseBlock[0].ShouldBeOfType<PrintNode>();
        node.ElseBlock[1].ShouldBeOfType<ThrowNode>();
    }

    /// <summary>
    /// Tests that a guard clause with an identifier condition produces a <see cref="GuardNode"/> with an <see cref="IdentifierNode"/> as the condition.
    /// </summary>
    [Fact]
    public void Parse_GuardClause_WithIdentifierCondition_ParsesIdentifierNode()
    {
        GuardNode node = this.ParseFirstStatement<GuardNode>(
            "YEOMAN isValid OTHERWISE, UNDER ORDERS.");

        node.Condition.ShouldBeOfType<IdentifierNode>().Name.ShouldBe("isValid");
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
