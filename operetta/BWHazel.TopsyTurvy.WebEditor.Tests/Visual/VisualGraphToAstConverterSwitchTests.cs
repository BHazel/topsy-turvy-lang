using System.Collections.Generic;
using System.Linq;
using Blazor.Diagrams;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.WebEditor.Visual;

namespace BWHazel.TopsyTurvy.WebEditor.Tests.Visual;

/// <summary>
/// Switch round-trip tests for the <see cref="VisualGraphToAstConverter"/> class.
/// </summary>
public class VisualGraphToAstConverterSwitchTests : VisualGraphToAstConverterTestBase
{
    /// <summary>
    /// Tests that a switch round-trips with its cases and default block preserved.
    /// </summary>
    [Fact]
    public void RoundTrip_Switch_PreservesCasesAndDefaultBlock()
    {
        SwitchNode switchNode = new()
        {
            Expression = new IdentifierNode { Name = "office", Span = PlaceholderSpan },
            Cases =
            [
                new SwitchCase("Private Secretary", [PrintStatement(1)]),
                new SwitchCase("Chancellor of the Exchequer", [PrintStatement(2)]),
            ],
            DefaultBlock = [PrintStatement(3)],
            Span = PlaceholderSpan,
        };

        ProgramNode reconstructed = RoundTrip(WrapInProgram(switchNode));

        SwitchNode result = reconstructed.Statements.OfType<SwitchNode>().Single();
        result.Cases.Count.ShouldBe(2);
        result.Cases[0].Literal.ShouldBe("Private Secretary");
        result.Cases[0].Block.Count.ShouldBe(1);
        result.Cases[1].Literal.ShouldBe("Chancellor of the Exchequer");
        result.DefaultBlock.Count.ShouldBe(1);
    }

    /// <summary>
    /// Tests that a switch node with no original AST link, built from scratch in the Visual Editor, not
    /// loaded from a <c>.topsy</c> file, reconstructs its cases and default block via the factory path,
    /// rather than collapsing to a <see cref="BreakNode"/>.
    /// </summary>
    [Fact]
    public void Factory_SwitchOpener_ReconstructsCasesAndDefaultInsteadOfCollapsingToBreak()
    {
        SwitchNode switchNode = new()
        {
            Expression = new IdentifierNode { Name = "office", Span = PlaceholderSpan },
            Cases = [new SwitchCase("Private Secretary", [PrintStatement(1)])],
            DefaultBlock = [PrintStatement(2)],
            Span = PlaceholderSpan,
        };

        ProgramNode reconstructed = RoundTripAsFactory(WrapInProgram(switchNode), "SwitchOpener");

        reconstructed.Statements.OfType<BreakNode>().ShouldBeEmpty();
        SwitchNode result = reconstructed.Statements.OfType<SwitchNode>().Single();
        result.Cases.Count.ShouldBe(1);
        result.Cases[0].Literal.ShouldBe("Private Secretary");
        result.Cases[0].Block.Count.ShouldBe(1);
        result.DefaultBlock.Count.ShouldBe(1);
    }

    /// <summary>
    /// Tests that a switch block built entirely from <see cref="VisualNodeFactory"/> exports its first case with the factory default literal,
    /// rather than an empty or <c>null</c> value.
    /// </summary>
    [Fact]
    public void Factory_SwitchBlockFromNodeFactory_FirstCaseHasDefaultLiteral()
    {
        BlazorDiagram diagram = Build(WrapInProgram());
        TopsyTurvyVisualNodeModel hark = diagram.Nodes.OfType<TopsyTurvyVisualNodeModel>().Single(node => node.StatementType == "HarkNode");
        TopsyTurvyVisualNodeModel finale = diagram.Nodes.OfType<TopsyTurvyVisualNodeModel>().Single(node => node.StatementType == "FinaleNode");
        diagram.Links.Clear();

        int nodeCounter = 0;
        IReadOnlyList<TopsyTurvyVisualNodeModel> switchNodes = VisualNodeFactory.CreateStatement("SwitchOpener", new(0, 0), diagram, ref nodeCounter);
        TopsyTurvyVisualNodeModel opener = switchNodes[0];
        TopsyTurvyVisualNodeModel closer = switchNodes[^1];

        diagram.Links.Add(new LinkModel(
            hark.Ports.OfType<TopsyTurvyVisualPortModel>().Single(port => port.Role == VisualPortRole.FlowOut),
            opener.Ports.OfType<TopsyTurvyVisualPortModel>().Single(port => port.Role == VisualPortRole.FlowIn)));
        diagram.Links.Add(new LinkModel(
            closer.Ports.OfType<TopsyTurvyVisualPortModel>().Single(port => port.Role == VisualPortRole.FlowOut),
            finale.Ports.OfType<TopsyTurvyVisualPortModel>().Single(port => port.Role == VisualPortRole.FlowIn)));

        ProgramNode reconstructed = Convert(diagram);

        SwitchNode result = reconstructed.Statements.OfType<SwitchNode>().Single();
        result.Cases.Count.ShouldBe(1);
        result.Cases[0].Literal.ShouldBe(1);
    }

    /// <summary>
    /// Tests that editing a case header literal value on the diagram after loading from source is
    /// honoured, rather than being overridden by the original, frozen AST literal.
    /// </summary>
    [Fact]
    public void RoundTrip_CaseLiteralEditedAfterLoad_ReflectsEditedValueNotOriginalAst()
    {
        SwitchNode switchNode = new()
        {
            Expression = new IdentifierNode { Name = "office", Span = PlaceholderSpan },
            Cases = [new SwitchCase("Private Secretary", [PrintStatement(1)])],
            Span = PlaceholderSpan,
        };

        BlazorDiagram diagram = Build(WrapInProgram(switchNode));
        TopsyTurvyVisualNodeModel header = diagram.Nodes.OfType<TopsyTurvyVisualNodeModel>().Single(node => node.StatementType == "SwitchCaseBranch");
        header.NodeLiteralType = LiteralType.Integer;
        header.LiteralValue = "42";

        ProgramNode reconstructed = Convert(diagram);

        SwitchNode result = reconstructed.Statements.OfType<SwitchNode>().Single();
        result.Cases[0].Literal.ShouldBe(42);
    }

    /// <summary>
    /// Tests that a case added to the opener Branch Out ports after loading from source, simulating the
    /// "+ Case" button, is included in the reconstructed switch, rather than being silently dropped because
    /// it has no corresponding entry in the original, frozen AST <c>Cases</c> list.
    /// </summary>
    [Fact]
    public void RoundTrip_CaseAddedAfterLoad_IsIncludedAlongsideOriginalCases()
    {
        SwitchNode switchNode = new()
        {
            Expression = new IdentifierNode { Name = "office", Span = PlaceholderSpan },
            Cases = [new SwitchCase("Private Secretary", [PrintStatement(1)])],
            Span = PlaceholderSpan,
        };

        BlazorDiagram diagram = Build(WrapInProgram(switchNode));
        TopsyTurvyVisualNodeModel opener = diagram.Nodes.OfType<TopsyTurvyVisualNodeModel>().Single(node => node.StatementType == "SwitchOpener");

        TopsyTurvyVisualPortModel newCasePort = new(opener, PortAlignment.Bottom, "Case 2", VisualPortRole.BranchOut);
        opener.AddPort(newCasePort);

        TopsyTurvyVisualNodeModel newHeader = new("new-case-header", new Point(0, 0), "WHEN ACTING AS", "case 2", VisualNodeKind.Conditional)
        {
            StatementType = "SwitchCaseBranch",
            NodeLiteralType = LiteralType.Integer,
            LiteralValue = "2",
        };
        
        newHeader.AddPort(new TopsyTurvyVisualPortModel(newHeader, PortAlignment.Top, "In", VisualPortRole.FlowIn));
        diagram.Nodes.Add(newHeader);
        diagram.Links.Add(new LinkModel(newCasePort, newHeader.Ports.OfType<TopsyTurvyVisualPortModel>().Single(port => port.Role == VisualPortRole.FlowIn)));

        ProgramNode reconstructed = Convert(diagram);

        SwitchNode result = reconstructed.Statements.OfType<SwitchNode>().Single();
        result.Cases.Count.ShouldBe(2);
        result.Cases[0].Literal.ShouldBe("Private Secretary");
        result.Cases[1].Literal.ShouldBe(2);
    }
}
