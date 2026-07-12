using Blazor.Diagrams;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.WebEditor.Visual;

namespace BWHazel.TopsyTurvy.WebEditor.Tests.Visual;

/// <summary>
/// Guard tests for the <see cref="VisualGraphBuilder"/> class.
/// </summary>
public class VisualGraphBuilderGuardTests : VisualGraphBuilderTestBase
{
    /// <summary>
    /// Tests that a guard Success Flow Out port is linked directly to the closer Flow In port,
    /// unlike every other block type, which links its branch tail(s) to the closer instead.
    /// </summary>
    [Fact]
    public void Build_WithGuard_SuccessFlowOutLinksDirectlyToCloser()
    {
        GuardNode guard = new()
        {
            Condition = IntegerLiteral(1),
            ElseBlock = [],
            Span = PlaceholderSpan
        };

        BlazorDiagram diagram = Build(WrapInProgram(guard));

        TopsyTurvyVisualNodeModel opener = Node(diagram, "GuardOpener");
        TopsyTurvyVisualNodeModel closer = Node(diagram, "GuardCloser");
        IsLinked(Port(opener, "Success", VisualPortRole.FlowOut)).ShouldBeTrue();
        opener.PairedCloserId.ShouldBe(closer.Id);
        closer.PairedOpenerId.ShouldBe(opener.Id);
    }

    /// <summary>
    /// Tests that a guard else block creates a "Failure" Branch Out port wired to an "OTHERWISE," header.
    /// </summary>
    [Fact]
    public void Build_WithGuardElseBlock_CreatesFailureBranchPortWithOtherwiseHeader()
    {
        GuardNode guard = new()
        {
            Condition = IntegerLiteral(1),
            ElseBlock = [new PrintNode { Expression = IntegerLiteral(1), Span = PlaceholderSpan }],
            Span = PlaceholderSpan,
        };

        BlazorDiagram diagram = Build(WrapInProgram(guard));

        TopsyTurvyVisualNodeModel opener = Node(diagram, "GuardOpener");
        Port(opener, "Failure", VisualPortRole.BranchOut).ShouldNotBeNull();
        Node(diagram, "GuardElseBranch").Title.ShouldBe("OTHERWISE,");
    }

    /// <summary>
    /// Tests that a guard condition expression is wired to its "Cond" Data In port.
    /// </summary>
    [Fact]
    public void Build_WithGuardCondition_WiresCondPort()
    {
        GuardNode guard = new()
        {
            Condition = IntegerLiteral(1),
            ElseBlock = [],
            Span = PlaceholderSpan
        };

        BlazorDiagram diagram = Build(WrapInProgram(guard));

        IsLinked(Port(Node(diagram, "GuardOpener"), "Cond", VisualPortRole.DataIn)).ShouldBeTrue();
    }
}
