using System.Collections.Generic;
using System.Linq;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.WebEditor.Visual;

namespace BWHazel.TopsyTurvy.WebEditor.Tests.Visual;

/// <summary>
/// Operator round-trip tests for the <see cref="VisualGraphToAstConverter"/> class.
/// </summary>
public class VisualGraphToAstConverterOperatorTests : VisualGraphToAstConverterTestBase
{
    /// <summary>
    /// Tests that a Transposition node with no <c>BY</c> argument round-trips with its single argument preserved.
    /// </summary>
    [Theory]
    [InlineData(Operator.TranspositionUp)]
    [InlineData(Operator.TranspositionDown)]
    public void RoundTrip_PreservesSingleArgument_WhenByClauseOmitted(Operator prefixOperator)
    {
        ProgramNode program = CreatePrefixProgram(prefixOperator, [IntegerLiteral(7)]);

        PrefixExpressionNode prefixExpression = ExtractPrefixExpression(RoundTrip(program));

        prefixExpression.Operator.ShouldBe(prefixOperator);
        prefixExpression.Arguments.Count.ShouldBe(1);
        ((LiteralNode)prefixExpression.Arguments[0]).Value.ShouldBe(7);
    }

    /// <summary>
    /// Tests that a Transposition node with a <c>BY</c> argument round-trips with both arguments preserved in order.
    /// </summary>
    [Theory]
    [InlineData(Operator.TranspositionUp)]
    [InlineData(Operator.TranspositionDown)]
    public void RoundTrip_PreservesArgumentOrder_WhenByClausePresent(Operator prefixOperator)
    {
        ProgramNode program = CreatePrefixProgram(prefixOperator, [IntegerLiteral(7), IntegerLiteral(3)]);

        PrefixExpressionNode prefixExpression = ExtractPrefixExpression(RoundTrip(program));

        prefixExpression.Operator.ShouldBe(prefixOperator);
        prefixExpression.Arguments.Count.ShouldBe(2);
        ((LiteralNode)prefixExpression.Arguments[0]).Value.ShouldBe(7);
        ((LiteralNode)prefixExpression.Arguments[1]).Value.ShouldBe(3);
    }

    /// <summary>
    /// Creates a <see cref="ProgramNode"/> containing a single <see cref="PrefixExpressionNode"/> with the given operator and arguments.
    /// </summary>
    /// <param name="prefixOperator">The prefix operator.</param>
    /// <param name="arguments">The arguments for the prefix operator.</param>
    /// <returns>A <see cref="ProgramNode"/> containing the prefix expression.</returns>
    private static ProgramNode CreatePrefixProgram(Operator prefixOperator, IReadOnlyList<Expression> arguments)
    {
        PrefixExpressionNode prefixExpression = new()
        {
            Operator = prefixOperator,
            Arguments = arguments,
            Span = PlaceholderSpan,
        };

        PrintNode print = new()
        {
            Expression = prefixExpression,
            Span = PlaceholderSpan
        };

        return new()
        {
            Title = "Round Trip",
            Statements = [print],
            Span = PlaceholderSpan,
        };
    }

    /// <summary>
    /// Creates a <see cref="LiteralNode"/> representing an integer literal with the given value.
    /// </summary>
    /// <param name="value">The integer value.</param>
    /// <returns>A <see cref="LiteralNode"/> representing the integer literal.</returns>
    private static LiteralNode IntegerLiteral(int value) =>
        new()
        {
            Value = value,
            Type = LiteralType.Integer,
            Span = PlaceholderSpan
        };

    /// <summary>
    /// Extracts the <see cref="PrefixExpressionNode"/> from a <see cref="ProgramNode"/> containing a single <see cref="PrintNode"/>.
    /// </summary>
    /// <param name="program">The programme containing the print node.</param>
    /// <returns>The extracted <see cref="PrefixExpressionNode"/>.</returns>
    private static PrefixExpressionNode ExtractPrefixExpression(ProgramNode program)
    {
        PrintNode print = program.Statements.OfType<PrintNode>().Single();
        return (PrefixExpressionNode)print.Expression;
    }
}
