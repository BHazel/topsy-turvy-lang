using System.Linq;
using Blazor.Diagrams;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.WebEditor.Visual;

namespace BWHazel.TopsyTurvy.WebEditor.Tests.Visual;

/// <summary>
/// Function definition, parameter, and return tests for the <see cref="VisualGraphBuilder"/> class.
/// </summary>
public class VisualGraphBuilderFunctionTests : VisualGraphBuilderTestBase
{
    /// <summary>
    /// Tests that a function body is rendered as a separate subgraph, not as a node in the main flow.
    /// </summary>
    [Fact]
    public void Build_WithFunctionDefinition_RendersBodyAsSeparateSubgraphNotInMainFlow()
    {
        FunctionDefinitionNode function = new()
        {
            Name = "compute",
            Parameters = [],
            Body = [],
            Span = PlaceholderSpan
        };

        BlazorDiagram diagram = Build(WrapInProgram(function));

        Node(diagram, "FunctionBodyOpener").SymbolIdentifierNodeName.ShouldBe("compute");
        Node(diagram, "FunctionBodyCloser");
        Nodes(diagram, "FunctionSignatureNode").ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that a function definition nested inside another function body produces a signature-only
    /// node (no separate body subgraph), since only top-level function definitions are collected for
    /// deferred subgraph rendering.
    /// </summary>
    [Fact]
    public void Build_WithNestedFunctionDefinitionInsideAnotherFunctionBody_CreatesSignatureOnlyNode()
    {
        FunctionDefinitionNode innerFunction = new()
        {
            Name = "inner",
            Parameters = [],
            Body = [],
            Span = PlaceholderSpan
        };

        FunctionDefinitionNode outerFunction = new()
        {
            Name = "outer",
            Parameters = [],
            Body = [innerFunction],
            Span = PlaceholderSpan
        };

        BlazorDiagram diagram = Build(WrapInProgram(outerFunction));

        Node(diagram, "FunctionSignatureNode").SymbolIdentifierNodeName.ShouldBe("inner");
        Nodes(diagram, "FunctionBodyOpener").Count().ShouldBe(1);
    }

    /// <summary>
    /// Tests that each function parameter creates a linked TERM node wired to a "Param N" Data In port.
    /// </summary>
    [Fact]
    public void Build_WithFunctionParameters_CreatesTermNodesLinkedToParamPorts()
    {
        FunctionDefinitionNode function = new()
        {
            Name = "compute",
            Parameters = [new TypedParameter("x", LiteralType.Integer, PlaceholderSpan)],
            Body = [],
            Span = PlaceholderSpan,
        };

        BlazorDiagram diagram = Build(WrapInProgram(function));

        TopsyTurvyVisualNodeModel opener = Node(diagram, "FunctionBodyOpener");
        IsLinked(Port(opener, "Param 1", VisualPortRole.DataIn)).ShouldBeTrue();
        TopsyTurvyVisualNodeModel term = Node(diagram, "ParameterNode");
        term.SymbolIdentifierNodeName.ShouldBe("x");
        term.NodeLiteralType.ShouldBe(LiteralType.Integer);
    }

    /// <summary>
    /// Tests that a function with a return type includes the arrow and type keyword in its subtitle.
    /// </summary>
    [Fact]
    public void Build_WithFunctionReturnType_SubtitleIncludesArrowAndType()
    {
        FunctionDefinitionNode function = new()
        {
            Name = "compute",
            Parameters = [],
            ReturnType = LiteralType.Integer,
            Body = [],
            Span = PlaceholderSpan
        };

        BlazorDiagram diagram = Build(WrapInProgram(function));

        Node(diagram, "FunctionBodyOpener").Subtitle.ShouldBe("compute → PEER");
    }

    /// <summary>
    /// Tests that a function without a return type has a subtitle that is just its name.
    /// </summary>
    [Fact]
    public void Build_WithFunctionWithoutReturnType_SubtitleIsJustName()
    {
        FunctionDefinitionNode function = new()
        {
            Name = "compute",
            Parameters = [],
            Body = [],
            Span = PlaceholderSpan
        };

        BlazorDiagram diagram = Build(WrapInProgram(function));

        Node(diagram, "FunctionBodyOpener").Subtitle.ShouldBe("compute");
    }

    /// <summary>
    /// Tests that multiple function definitions stack their body subgraphs vertically, with the second
    /// positioned below the first.
    /// </summary>
    [Fact]
    public void Build_WithMultipleFunctionDefinitions_StacksBodySubgraphsVertically()
    {
        FunctionDefinitionNode first = new()
        {
            Name = "first",
            Parameters = [],
            Body = [new PrintNode { Expression = IntegerLiteral(1), Span = PlaceholderSpan }],
            Span = PlaceholderSpan,
        };

        FunctionDefinitionNode second = new()
        {
            Name = "second",
            Parameters = [],
            Body = [],
            Span = PlaceholderSpan
        };

        BlazorDiagram diagram = Build(WrapInProgram(first, second));

        TopsyTurvyVisualNodeModel firstOpener = Nodes(diagram, "FunctionBodyOpener").Single(node => node.SymbolIdentifierNodeName == "first");
        TopsyTurvyVisualNodeModel secondOpener = Nodes(diagram, "FunctionBodyOpener").Single(node => node.SymbolIdentifierNodeName == "second");
        secondOpener.Position.Y.ShouldBeGreaterThan(firstOpener.Position.Y);
    }

    /// <summary>
    /// Tests that a programme return and a function return share the same title but have distinct
    /// statement types.
    /// </summary>
    [Fact]
    public void Build_WithProgrammeReturnAndReturn_ShareTitleButDifferStatementType()
    {
        ProgrammeReturnNode programmeReturn = new() { Value = IntegerLiteral(1), Span = PlaceholderSpan };
        BlazorDiagram programmeDiagram = Build(WrapInProgram(programmeReturn));
        Node(programmeDiagram, "ProgrammeReturnNode").Title.ShouldBe("AND SO I FIND");

        FunctionDefinitionNode function = new()
        {
            Name = "f",
            Parameters = [],
            Body = [new ReturnNode { Value = IntegerLiteral(1), Span = PlaceholderSpan }],
            Span = PlaceholderSpan,
        };
        
        BlazorDiagram functionDiagram = Build(WrapInProgram(function));
        Node(functionDiagram, "ReturnNode").Title.ShouldBe("AND SO I FIND");
    }

    /// <summary>
    /// Tests that a return statement without a value has no "Value" Data In port.
    /// </summary>
    [Fact]
    public void Build_WithReturnWithoutValue_HasNoValuePort()
    {
        FunctionDefinitionNode function = new()
        {
            Name = "f",
            Parameters = [],
            Body = [new ReturnNode { Span = PlaceholderSpan }],
            Span = PlaceholderSpan,
        };

        BlazorDiagram diagram = Build(WrapInProgram(function));

        Port(Node(diagram, "ReturnNode"), "Value", VisualPortRole.DataIn).ShouldBeNull();
    }
}
