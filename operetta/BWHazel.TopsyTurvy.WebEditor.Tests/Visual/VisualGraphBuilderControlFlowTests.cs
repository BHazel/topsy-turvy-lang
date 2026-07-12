using Blazor.Diagrams;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.WebEditor.Visual;

namespace BWHazel.TopsyTurvy.WebEditor.Tests.Visual;

/// <summary>
/// Conditional, loop, break and continue tests for the <see cref="VisualGraphBuilder"/> class.
/// </summary>
public class VisualGraphBuilderControlFlowTests : VisualGraphBuilderTestBase
{
    /// <summary>
    /// Tests that a conditional creates the opener, true branch and closer nodes, linked by paired IDs.
    /// </summary>
    [Fact]
    public void Build_WithConditional_CreatesOpenerTrueBranchAndCloserLinkedByPairedIds()
    {
        ConditionalNode conditional = new()
        {
            Condition = IntegerLiteral(1),
            TrueBlock = [PrintStatement(1)],
            Span = PlaceholderSpan,
        };

        BlazorDiagram diagram = Build(WrapInProgram(conditional));

        TopsyTurvyVisualNodeModel opener = Node(diagram, "ConditionalOpener");
        TopsyTurvyVisualNodeModel closer = Node(diagram, "ConditionalCloser");
        opener.PairedCloserId.ShouldBe(closer.Id);
        closer.PairedOpenerId.ShouldBe(opener.Id);
    }

    /// <summary>
    /// Tests that a conditional with else-if and else branches creates all Branch Out ports and header nodes,
    /// with the expected header titles.
    /// </summary>
    [Fact]
    public void Build_WithConditionalElseIfAndElse_CreatesAllBranchOutPortsAndHeaderNodes()
    {
        ConditionalNode conditional = new()
        {
            Condition = IntegerLiteral(1),
            TrueBlock = [PrintStatement(1)],
            ElseIfs = [new ElseIfBranch(IntegerLiteral(2), [PrintStatement(2)])],
            ElseBlock = [PrintStatement(3)],
            Span = PlaceholderSpan,
        };

        BlazorDiagram diagram = Build(WrapInProgram(conditional));

        TopsyTurvyVisualNodeModel opener = Node(diagram, "ConditionalOpener");
        Port(opener, "True", VisualPortRole.BranchOut).ShouldNotBeNull();
        Port(opener, "Else If 1", VisualPortRole.BranchOut).ShouldNotBeNull();
        Port(opener, "Else", VisualPortRole.BranchOut).ShouldNotBeNull();
        Node(diagram, "ConditionalTrueBranch").Title.ShouldBe("QUITE SO.");
        Node(diagram, "ConditionalElseIfBranch").Title.ShouldBe("OR, IF NOT,");
        Node(diagram, "ConditionalElseBranch").Title.ShouldBe("OTHERWISE,");
    }

    /// <summary>
    /// Tests that each loop type sets the opener subtitle to the type name.
    /// </summary>
    [Theory]
    [InlineData(LoopType.Infinite, "Infinite")]
    [InlineData(LoopType.Ascending, "Ascending")]
    [InlineData(LoopType.Descending, "Descending")]
    [InlineData(LoopType.Whilst, "Whilst")]
    public void Build_WithLoop_EachLoopType_SetsSubtitleToTypeName(LoopType type, string expectedSubtitle)
    {
        LoopNode loop = new()
        {
            Type = type,
            Body = [],
            Span = PlaceholderSpan
        };

        BlazorDiagram diagram = Build(WrapInProgram(loop));

        Node(diagram, "LoopOpener").Subtitle.ShouldBe(expectedSubtitle);
    }

    /// <summary>
    /// Tests that a loop with a condition gets a linked condition Data In port.
    /// </summary>
    [Fact]
    public void Build_WithLoopCondition_AddsLinkedCondDataInPort()
    {
        LoopNode loop = new() {
            Type = LoopType.Whilst,
            Condition = IntegerLiteral(1),
            Body = [],
            Span = PlaceholderSpan
        };

        BlazorDiagram diagram = Build(WrapInProgram(loop));

        IsLinked(Port(Node(diagram, "LoopOpener"), "Cond", VisualPortRole.DataIn)).ShouldBeTrue();
    }

    /// <summary>
    /// Tests that a loop with a step expression gets a linked step Data In port.
    /// </summary>
    [Fact]
    public void Build_WithLoopStep_AddsLinkedStepDataInPort()
    {
        LoopNode loop = new()
        {
            Type = LoopType.Ascending,
            Step = IntegerLiteral(1),
            Body = [],
            Span = PlaceholderSpan
        };

        BlazorDiagram diagram = Build(WrapInProgram(loop));

        IsLinked(Port(Node(diagram, "LoopOpener"), "Step", VisualPortRole.DataIn)).ShouldBeTrue();
    }

    /// <summary>
    /// Tests that a loop with neither a condition nor a step has neither Data In port.
    /// </summary>
    [Fact]
    public void Build_WithLoopWithoutConditionOrStep_HasNeitherPort()
    {
        LoopNode loop = new()
        {
            Type = LoopType.Infinite,
            Body = [],
            Span = PlaceholderSpan
        };

        BlazorDiagram diagram = Build(WrapInProgram(loop));

        TopsyTurvyVisualNodeModel opener = Node(diagram, "LoopOpener");
        Port(opener, "Cond", VisualPortRole.DataIn).ShouldBeNull();
        Port(opener, "Step", VisualPortRole.DataIn).ShouldBeNull();
    }

    /// <summary>
    /// Tests that a break statement node has no Flow Out port, since it never flows onward.
    /// </summary>
    [Fact]
    public void Build_WithBreak_HasNoFlowOutPort()
    {
        BreakNode statement = new()
        {
            Span = PlaceholderSpan
        };

        BlazorDiagram diagram = Build(WrapInProgram(statement));

        Port(Node(diagram, "BreakNode"), "Out", VisualPortRole.FlowOut).ShouldBeNull();
    }

    /// <summary>
    /// Tests that a continue statement node has no Flow Out port, since it never flows onward.
    /// </summary>
    [Fact]
    public void Build_WithContinue_HasNoFlowOutPort()
    {
        ContinueNode statement = new()
        {
            Span = PlaceholderSpan
        };

        BlazorDiagram diagram = Build(WrapInProgram(statement));

        Port(Node(diagram, "ContinueNode"), "Out", VisualPortRole.FlowOut).ShouldBeNull();
    }

    /// <summary>
    /// Creates a <see cref="PrintNode"/> with the given integer literal value.
    /// </summary>
    /// <param name="value">The integer value for the print statement.</param>
    /// <returns>A <see cref="PrintNode"/> representing the print statement.</returns>
    private static PrintNode PrintStatement(int value) => new()
    {
        Expression = IntegerLiteral(value),
        Span = PlaceholderSpan
    };
}
