using System.Linq;
using Blazor.Diagrams;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.WebEditor.Visual;

namespace BWHazel.TopsyTurvy.WebEditor.Tests.Visual;

/// <summary>
/// Guard round-trip tests for the <see cref="VisualGraphToAstConverter"/> class.
/// </summary>
public class VisualGraphToAstConverterGuardTests : VisualGraphToAstConverterTestBase
{
    /// <summary>
    /// Tests that a guard round-trips with its condition and else block preserved.
    /// </summary>
    [Fact]
    public void RoundTrip_Guard_PreservesConditionAndElseBlock()
    {
        GuardNode guard = new()
        {
            Condition = new LiteralNode { Value = true, Type = LiteralType.Boolean, Span = PlaceholderSpan },
            ElseBlock = [new PrintNode { Expression = new LiteralNode { Value = 1, Type = LiteralType.Integer, Span = PlaceholderSpan }, Span = PlaceholderSpan }],
            Span = PlaceholderSpan,
        };

        ProgramNode reconstructed = RoundTrip(WrapInProgram(guard));

        GuardNode result = reconstructed.Statements.OfType<GuardNode>().Single();
        result.Condition.ShouldNotBeNull();
        result.ElseBlock.Count.ShouldBe(1);
    }

    /// <summary>
    /// Tests that a guard node with no original AST link is reconstructed via the factory path.
    /// </summary>
    /// <remarks>
    /// <see cref="VisualGraphBuilder"/> always labels a guard else Branch Out port "Failure", but
    /// <c>VisualNodeFactory.CreateGuardBlock</c>m used when a guard is added fresh from the toolbox with
    /// no backing AST, labels the equivalent port "Else" instead: the factory reconstruction path reads
    /// specifically from "Else".  The port label is relabelled here to match what a toolbox-created guard
    /// would actually look like, since <see cref="VisualGraphBuilder"/> has no way to produce that shape directly.
    /// </remarks>
    [Fact]
    public void Factory_GuardOpener_ReconstructsElseBlockWhenBranchPortIsLabelledElse()
    {
        GuardNode guard = new()
        {
            Condition = new LiteralNode { Value = true, Type = LiteralType.Boolean, Span = PlaceholderSpan },
            ElseBlock = [new PrintNode { Expression = new LiteralNode { Value = 1, Type = LiteralType.Integer, Span = PlaceholderSpan }, Span = PlaceholderSpan }],
            Span = PlaceholderSpan,
        };
        
        BlazorDiagram diagram = Build(WrapInProgram(guard));

        TopsyTurvyVisualNodeModel opener = diagram.Nodes.OfType<TopsyTurvyVisualNodeModel>().Single(node => node.StatementType == "GuardOpener");
        opener.AstNode = null;
        opener.Ports.OfType<TopsyTurvyVisualPortModel>().Single(port => port.Role == VisualPortRole.BranchOut).Label = "Else";

        ProgramNode reconstructed = Convert(diagram);

        reconstructed.Statements.OfType<GuardNode>().Single().ElseBlock.Count.ShouldBe(1);
    }
}
