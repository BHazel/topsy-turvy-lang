using System.Linq;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.WebEditor.Visual;

namespace BWHazel.TopsyTurvy.WebEditor.Tests.Visual;

/// <summary>
/// Assert round-trip tests for the <see cref="VisualGraphToAstConverter"/> class.
/// </summary>
public class VisualGraphToAstConverterAssertTests : VisualGraphToAstConverterTestBase
{
    /// <summary>
    /// Tests that an assert statement round-trips with its condition and error message preserved in order.
    /// </summary>
    [Fact]
    public void RoundTrip_Assert_PreservesConditionAndErrorMessage()
    {
        AssertNode statement = new()
        {
            Condition = new LiteralNode()
            {
                Value = true, Type = LiteralType.Boolean, Span = PlaceholderSpan
            },
            ErrorMessage = new LiteralNode()
            {
                Value = "failed", Type = LiteralType.String, Span = PlaceholderSpan
            },
            Span = PlaceholderSpan,
        };

        ProgramNode reconstructed = RoundTrip(WrapInProgram(statement));

        AssertNode result = reconstructed.Statements.OfType<AssertNode>().Single();
        ((LiteralNode)result.Condition).Value.ShouldBe(true);
        ((LiteralNode)result.ErrorMessage).Value.ShouldBe("failed");
    }
}
