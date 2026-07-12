using System.Linq;
using Blazor.Diagrams;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.WebEditor.Visual;

namespace BWHazel.TopsyTurvy.WebEditor.Tests.Visual;

/// <summary>
/// Tests for the <see cref="VisualGraphBuilder"/> class.
/// </summary>
public class VisualGraphBuilderAssertTests : VisualGraphBuilderTestBase
{
    /// <summary>
    /// Assert tests that an assert statement wires both its Cond and Msg Data In ports.
    /// </summary>
    [Fact]
    public void Build_WithAssert_WiresCondAndMsgPorts()
    {
        AssertNode statement = new()
        {
            Condition = IntegerLiteral(1),
            ErrorMessage = IntegerLiteral(2),
            Span = PlaceholderSpan
        };

        BlazorDiagram diagram = Build(WrapInProgram(statement));

        TopsyTurvyVisualNodeModel node = Node(diagram, "AssertNode");
        IsLinked(Port(node, "Cond", VisualPortRole.DataIn)).ShouldBeTrue();
        IsLinked(Port(node, "Msg", VisualPortRole.DataIn)).ShouldBeTrue();
    }

    /// <summary>
    /// Tests that the Cond expression is placed at port index 0 and the Msg expression at port index 1,
    /// so the Msg expression's node is offset further down than the Cond expression's node.
    /// </summary>
    [Fact]
    public void Build_WithAssert_MsgExpressionIsVerticallyOffsetBelowCondExpression()
    {
        AssertNode statement = new()
        {
            Condition = IntegerLiteral(1),
            ErrorMessage = IntegerLiteral(2),
            Span = PlaceholderSpan
        };

        BlazorDiagram diagram = Build(WrapInProgram(statement));

        TopsyTurvyVisualNodeModel condLiteral = diagram.Nodes.OfType<TopsyTurvyVisualNodeModel>()
            .Single(node => node.StatementType == "LiteralNode" && node.LiteralValue == "1");
        TopsyTurvyVisualNodeModel msgLiteral = diagram.Nodes.OfType<TopsyTurvyVisualNodeModel>()
            .Single(node => node.StatementType == "LiteralNode" && node.LiteralValue == "2");
        msgLiteral.Position.Y.ShouldBeGreaterThan(condLiteral.Position.Y);
    }
}
