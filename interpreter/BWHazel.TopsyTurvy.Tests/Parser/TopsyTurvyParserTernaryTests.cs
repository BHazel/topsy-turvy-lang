using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Parser;

namespace BWHazel.TopsyTurvy.Tests.Parser;

/// <summary>
/// Tests for ternary expression forms parsed by the <see cref="TopsyTurvyParser"/> class.
/// </summary>
public class TopsyTurvyParserTernaryTests
{
    private readonly TopsyTurvyParser parser = new();

    /// <summary>
    /// Tests that a ternary expression in an assignment produces a <see cref="TernaryExpressionNode"/> with <see cref="TernaryExpressionNode.TrueValue"/>, <see cref="TernaryExpressionNode.Condition"/> and <see cref="TernaryExpressionNode.FalseValue"/> set correctly.
    /// </summary>
    [Fact]
    public void Parse_TernaryExpression_InAssignment_ParsesAllThreeParts()
    {
        AssignmentNode node = this.ParseFirstStatement<AssignmentNode>(
            """result IS APPOINTED "yes" SHOULD IT TRANSPIRE THAT VERITY OTHERWISE, "no" """);

        TernaryExpressionNode ternary = node.Value.ShouldBeOfType<TernaryExpressionNode>();
        LiteralNode trueValue = ternary.TrueValue.ShouldBeOfType<LiteralNode>();
        trueValue.Value.ShouldBe("yes");
        trueValue.Type.ShouldBe(LiteralType.String);
        LiteralNode condition = ternary.Condition.ShouldBeOfType<LiteralNode>();
        condition.Value.ShouldBe(true);
        LiteralNode falseValue = ternary.FalseValue.ShouldBeOfType<LiteralNode>();
        falseValue.Value.ShouldBe("no");
        falseValue.Type.ShouldBe(LiteralType.String);
    }

    /// <summary>
    /// Tests that a ternary expression using a comparison as the condition produces a <see cref="TernaryExpressionNode"/> with a <see cref="PrefixExpressionNode"/> condition.
    /// </summary>
    [Fact]
    public void Parse_TernaryExpression_WithComparisonCondition_ParsesConditionAsPrefixExpression()
    {
        AssignmentNode node = this.ParseFirstStatement<AssignmentNode>(
            "label IS APPOINTED 1 SHOULD IT TRANSPIRE THAT ALIKE score AND 100 OTHERWISE, 0");

        TernaryExpressionNode ternary = node.Value.ShouldBeOfType<TernaryExpressionNode>();
        PrefixExpressionNode condition = ternary.Condition.ShouldBeOfType<PrefixExpressionNode>();
        condition.Operator.ShouldBe(Operator.Alike);
        condition.Arguments.Count.ShouldBe(2);
    }

    /// <summary>
    /// Tests that a ternary expression as the value in a <c>BEHOLD</c> statement produces a <see cref="TernaryExpressionNode"/> on the <see cref="PrintNode"/>.
    /// </summary>
    [Fact]
    public void Parse_TernaryExpression_InBehold_ParsesAsPrintNodeExpression()
    {
        PrintNode node = this.ParseFirstStatement<PrintNode>(
            """BEHOLD "Prime" SHOULD IT TRANSPIRE THAT VERITY OTHERWISE, "Not prime" """);

        TernaryExpressionNode ternary = node.Expression.ShouldBeOfType<TernaryExpressionNode>();
        ternary.TrueValue.ShouldBeOfType<LiteralNode>().Value.ShouldBe("Prime");
        ternary.FalseValue.ShouldBeOfType<LiteralNode>().Value.ShouldBe("Not prime");
    }

    /// <summary>
    /// Tests that a ternary expression with a right-nested ternary in the false value position produces a <see cref="TernaryExpressionNode"/> whose <see cref="TernaryExpressionNode.FalseValue"/> is also a <see cref="TernaryExpressionNode"/>.
    /// </summary>
    [Fact]
    public void Parse_TernaryExpression_RightNestedInFalseValue_ProducesNestedNode()
    {
        AssignmentNode node = this.ParseFirstStatement<AssignmentNode>(
            """rank IS APPOINTED "Senior" SHOULD IT TRANSPIRE THAT VERITY OTHERWISE, "Junior" SHOULD IT TRANSPIRE THAT NAY OTHERWISE, "Mid" """);

        TernaryExpressionNode outer = node.Value.ShouldBeOfType<TernaryExpressionNode>();
        outer.TrueValue.ShouldBeOfType<LiteralNode>().Value.ShouldBe("Senior");
        TernaryExpressionNode inner = outer.FalseValue.ShouldBeOfType<TernaryExpressionNode>();
        inner.TrueValue.ShouldBeOfType<LiteralNode>().Value.ShouldBe("Junior");
        inner.FalseValue.ShouldBeOfType<LiteralNode>().Value.ShouldBe("Mid");
    }

    /// <summary>
    /// Tests that a ternary expression with an identifier as the true value produces a <see cref="TernaryExpressionNode"/> whose <see cref="TernaryExpressionNode.TrueValue"/> is an <see cref="IdentifierNode"/>.
    /// </summary>
    [Fact]
    public void Parse_TernaryExpression_WithIdentifierTrueValue_ParsesIdentifierNode()
    {
        AssignmentNode node = this.ParseFirstStatement<AssignmentNode>(
            "output IS APPOINTED defaultValue SHOULD IT TRANSPIRE THAT NAY OTHERWISE, 0");

        TernaryExpressionNode ternary = node.Value.ShouldBeOfType<TernaryExpressionNode>();
        ternary.TrueValue.ShouldBeOfType<IdentifierNode>().Name.ShouldBe("defaultValue");
    }

    /// <summary>
    /// Tests that a ternary expression in a variable declaration initial value produces a <see cref="TernaryExpressionNode"/> on the <see cref="DeclarationNode"/>.
    /// </summary>
    [Fact]
    public void Parse_TernaryExpression_InDeclarationInitialValue_ParsesCorrectly()
    {
        DeclarationNode node = this.ParseFirstStatement<DeclarationNode>(
            """PRAY WELCOME title AS A YARN BEING "Yes" SHOULD IT TRANSPIRE THAT VERITY OTHERWISE, "No" """);

        TernaryExpressionNode ternary = node.InitialValue.ShouldBeOfType<TernaryExpressionNode>();
        ternary.TrueValue.ShouldBeOfType<LiteralNode>().Value.ShouldBe("Yes");
        ternary.FalseValue.ShouldBeOfType<LiteralNode>().Value.ShouldBe("No");
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
