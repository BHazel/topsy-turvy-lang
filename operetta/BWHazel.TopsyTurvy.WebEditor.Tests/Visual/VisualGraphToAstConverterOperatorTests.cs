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
        PrefixExpressionNode prefix = new() { Operator = prefixOperator, Arguments = [IntegerLiteral(7)], Span = PlaceholderSpan };

        PrefixExpressionNode prefixExpression = (PrefixExpressionNode)ExtractExpression(RoundTripExpression(prefix));

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
        PrefixExpressionNode prefix = new() { Operator = prefixOperator, Arguments = [IntegerLiteral(7), IntegerLiteral(3)], Span = PlaceholderSpan };

        PrefixExpressionNode prefixExpression = (PrefixExpressionNode)ExtractExpression(RoundTripExpression(prefix));

        prefixExpression.Operator.ShouldBe(prefixOperator);
        prefixExpression.Arguments.Count.ShouldBe(2);
        ((LiteralNode)prefixExpression.Arguments[0]).Value.ShouldBe(7);
        ((LiteralNode)prefixExpression.Arguments[1]).Value.ShouldBe(3);
    }
}
