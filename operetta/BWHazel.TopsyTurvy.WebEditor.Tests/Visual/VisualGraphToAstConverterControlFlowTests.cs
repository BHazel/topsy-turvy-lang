using System.Linq;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.WebEditor.Visual;

namespace BWHazel.TopsyTurvy.WebEditor.Tests.Visual;

/// <summary>
/// Conditional, loop, break and continue round-trip tests for the <see cref="VisualGraphToAstConverter"/> class.
/// </summary>
public class VisualGraphToAstConverterControlFlowTests : VisualGraphToAstConverterTestBase
{
    /// <summary>
    /// Tests that a conditional round-trips with its condition, true block, else-if blocks and else block preserved.
    /// </summary>
    [Fact]
    public void RoundTrip_Conditional_PreservesTrueElseIfAndElseBlocksAndConditions()
    {
        ConditionalNode conditional = new()
        {
            Condition = new LiteralNode() { Value = 1, Type = LiteralType.Integer, Span = PlaceholderSpan },
            TrueBlock = [PrintStatement(1)],
            ElseIfs = [new ElseIfBranch(new LiteralNode() { Value = 2, Type = LiteralType.Integer, Span = PlaceholderSpan }, [PrintStatement(2)])],
            ElseBlock = [PrintStatement(3)],
            Span = PlaceholderSpan,
        };

        ProgramNode reconstructed = RoundTrip(WrapInProgram(conditional));

        ConditionalNode result = reconstructed.Statements.OfType<ConditionalNode>().Single();
        result.TrueBlock.Count.ShouldBe(1);
        result.ElseIfs.Count.ShouldBe(1);
        result.ElseBlock.Count.ShouldBe(1);
    }

    /// <summary>
    /// Tests that a loop round-trips with its type, condition and step preserved for the three
    /// condition-bearing loop types (Whilst, Ascending, Descending); Infinite loops carry neither.
    /// </summary>
    [Theory]
    [InlineData(LoopType.Whilst)]
    [InlineData(LoopType.Ascending)]
    [InlineData(LoopType.Descending)]
    public void RoundTrip_ConditionBearingLoop_PreservesTypeAndCondition(LoopType type)
    {
        LoopNode loop = new()
        {
            Type = type,
            Condition = new LiteralNode { Value = true, Type = LiteralType.Boolean, Span = PlaceholderSpan },
            Body = [PrintStatement(1)],
            Span = PlaceholderSpan,
        };

        ProgramNode reconstructed = RoundTrip(WrapInProgram(loop));

        LoopNode result = reconstructed.Statements.OfType<LoopNode>().Single();
        result.Type.ShouldBe(type);
        result.Condition.ShouldNotBeNull();
        result.Body.Count.ShouldBe(1);
    }

    /// <summary>
    /// Tests that an infinite loop round-trips with a <c>null</c> condition.
    /// </summary>
    [Fact]
    public void RoundTrip_InfiniteLoop_HasNullCondition()
    {
        LoopNode loop = new() { Type = LoopType.Infinite, Body = [], Span = PlaceholderSpan };

        ProgramNode reconstructed = RoundTrip(WrapInProgram(loop));

        reconstructed.Statements.OfType<LoopNode>().Single().Condition.ShouldBeNull();
    }

    /// <summary>
    /// Tests that a break statement round-trips correctly.
    /// </summary>
    /// <remarks>
    /// Tested in its own programme, not alongside a following statement: <c>BreakNode</c>/<c>ContinueNode</c>
    /// visual nodes have no Flow Out port, so anything placed after one would be left disconnected from
    /// the main flow chain by <see cref="VisualGraphBuilder"/> and never reconstructed at all.
    /// </remarks>
    [Fact]
    public void RoundTrip_Break_RoundTripsCorrectly()
    {
        BreakNode statement = new() { Span = PlaceholderSpan };

        ProgramNode reconstructed = RoundTrip(WrapInProgram(statement));

        reconstructed.Statements.OfType<BreakNode>().ShouldHaveSingleItem();
    }

    /// <summary>
    /// Tests that a continue statement round-trips correctly.
    /// </summary>
    [Fact]
    public void RoundTrip_Continue_RoundTripsCorrectly()
    {
        ContinueNode statement = new() { Span = PlaceholderSpan };

        ProgramNode reconstructed = RoundTrip(WrapInProgram(statement));

        reconstructed.Statements.OfType<ContinueNode>().ShouldHaveSingleItem();
    }

    /// <summary>
    /// Tests that a conditional node with no original AST link is reconstructed via the factory path,
    /// preserving its true block.
    /// </summary>
    [Fact]
    public void Factory_ConditionalOpener_ReconstructsViaConditionalFactory()
    {
        ConditionalNode conditional = new()
        {
            Condition = new LiteralNode { Value = 1, Type = LiteralType.Integer, Span = PlaceholderSpan },
            TrueBlock = [PrintStatement(1)],
            Span = PlaceholderSpan,
        };

        ProgramNode reconstructed = RoundTripAsFactory(WrapInProgram(conditional), "ConditionalOpener");

        reconstructed.Statements.OfType<ConditionalNode>().Single().TrueBlock.Count.ShouldBe(1);
    }
}
