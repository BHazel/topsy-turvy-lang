using System.Linq;
using Blazor.Diagrams;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.WebEditor.Visual;

namespace BWHazel.TopsyTurvy.WebEditor.Tests.Visual;

/// <summary>
/// Expression tests for the <see cref="VisualGraphBuilder"/> class.
/// </summary>
public class VisualGraphBuilderExpressionTests : VisualGraphBuilderTestBase
{
    /// <summary>
    /// Tests that each operator family maps its prefix expression to the correct statement type and title.
    /// </summary>
    /// <param name="prefixOperator">The operator to test.</param>
    /// <param name="expectedStatementType">The expected statement type for the operator.</param>
    /// <param name="expectedTitle">The expected title for the operator.</param>
    [Theory]
    [InlineData(Operator.Sum, "ArithmeticNode", "SUM OF")]
    [InlineData(Operator.ChordOf, "BitwiseNode", "CHORD OF")]
    [InlineData(Operator.Both, "LogicalNode", "BOTH")]
    [InlineData(Operator.AllOf, "VariadicNode", "ALL OF")]
    [InlineData(Operator.WovenOf, "WovenNode", "WOVEN OF")]
    public void Build_WithOperatorExpression_MapsToCorrectStatementTypeAndTitle(Operator prefixOperator, string expectedStatementType, string expectedTitle)
    {
        PrefixExpressionNode prefix = new()
        {
            Operator = prefixOperator,
            Arguments = [IntegerLiteral(1), IntegerLiteral(2)],
            Span = PlaceholderSpan
        };

        BlazorDiagram diagram = BuildExpression(prefix);

        Node(diagram, expectedStatementType).Title.ShouldBe(expectedTitle);
    }

    /// <summary>
    /// Tests that a ternary expression creates Cond, True and False Data In ports, all linked.
    /// </summary>
    [Fact]
    public void Build_WithTernary_CreatesCondTrueFalseDataInPortsAllWired()
    {
        TernaryExpressionNode ternary = new()
        {
            Condition = IntegerLiteral(1),
            TrueValue = IntegerLiteral(2),
            FalseValue = IntegerLiteral(3),
            Span = PlaceholderSpan
        };

        BlazorDiagram diagram = BuildExpression(ternary);

        TopsyTurvyVisualNodeModel node = Node(diagram, "TernaryNode");
        IsLinked(Port(node, "Cond", VisualPortRole.DataIn)).ShouldBeTrue();
        IsLinked(Port(node, "True", VisualPortRole.DataIn)).ShouldBeTrue();
        IsLinked(Port(node, "False", VisualPortRole.DataIn)).ShouldBeTrue();
    }

    /// <summary>
    /// Tests that an array index expression creates a separate identifier node for the array name,
    /// distinct from the index node itself.
    /// </summary>
    [Fact]
    public void Build_WithArrayIndex_CreatesSeparateIdentifierNodeForArrayName()
    {
        ArrayIndexNode arrayIndex = new()
        {
            ArrayName = "items",
            Index = IntegerLiteral(0),
            Span = PlaceholderSpan
        };

        BlazorDiagram diagram = BuildExpression(arrayIndex);

        TopsyTurvyVisualNodeModel indexNode = Node(diagram, "ArrayIndexNode");
        IsLinked(Port(indexNode, "Variable", VisualPortRole.DataIn)).ShouldBeTrue();
        IsLinked(Port(indexNode, "Index", VisualPortRole.DataIn)).ShouldBeTrue();
        TopsyTurvyVisualNodeModel identifierNode = Nodes(diagram, "IdentifierNode").Single(node => node.SymbolIdentifierNodeName == "items");
        identifierNode.Title.ShouldBe("items");
    }

    /// <summary>
    /// Tests that an array length expression wires its Variable Data In port to an identifier node.
    /// </summary>
    [Fact]
    public void Build_WithArrayLength_WiresVariablePortToIdentifierNode()
    {
        ArrayLengthNode arrayLength = new()
        {
            ArrayName = "items",
            Span = PlaceholderSpan
        };

        BlazorDiagram diagram = BuildExpression(arrayLength);

        IsLinked(Port(Node(diagram, "ArrayLengthNode"), "Variable", VisualPortRole.DataIn)).ShouldBeTrue();
    }

    /// <summary>
    /// Tests that an address-of expression creates a separate identifier node for the target variable,
    /// distinct from the address-of node itself, wired to its Variable Data In port.
    /// </summary>
    [Fact]
    public void Build_WithAddressOf_CreatesSeparateIdentifierNodeForTargetVariable()
    {
        AddressOfExpressionNode addressOf = new()
        {
            VariableName = "Number",
            Span = PlaceholderSpan
        };

        BlazorDiagram diagram = BuildExpression(addressOf);

        TopsyTurvyVisualNodeModel node = Node(diagram, "AddressOfExpressionNode");
        IsLinked(Port(node, "Variable", VisualPortRole.DataIn)).ShouldBeTrue();
        TopsyTurvyVisualNodeModel identifierNode = Nodes(diagram, "IdentifierNode").Single(n => n.SymbolIdentifierNodeName == "Number");
        identifierNode.Title.ShouldBe("Number");
    }

    /// <summary>
    /// Tests that a dereference expression creates a separate identifier node for the pointer variable,
    /// distinct from the dereference node itself, wired to its Pointer Data In port.
    /// </summary>
    [Fact]
    public void Build_WithDereference_CreatesSeparateIdentifierNodeForPointerVariable()
    {
        DereferenceExpressionNode dereference = new()
        {
            PointerName = "NumberPointer",
            Span = PlaceholderSpan
        };

        BlazorDiagram diagram = BuildExpression(dereference);

        TopsyTurvyVisualNodeModel node = Node(diagram, "DereferenceExpressionNode");
        IsLinked(Port(node, "Pointer", VisualPortRole.DataIn)).ShouldBeTrue();
        TopsyTurvyVisualNodeModel identifierNode = Nodes(diagram, "IdentifierNode").Single(n => n.SymbolIdentifierNodeName == "NumberPointer");
        identifierNode.Title.ShouldBe("NumberPointer");
    }

    /// <summary>
    /// Tests that an expression cast subtitle shows the arrow and target type keyword.
    /// </summary>
    [Fact]
    public void Build_WithExpressionCast_SubtitleShowsArrowAndTargetType()
    {
        ExpressionCastNode cast = new()
        {
            Expression = IntegerLiteral(1),
            NewType = LiteralType.Double,
            Span = PlaceholderSpan
        };

        BlazorDiagram diagram = BuildExpression(cast);

        Node(diagram, "ExpressionCastNode").Subtitle.ShouldBe("→ FATHOM");
    }

    /// <summary>
    /// Tests that a SUMMON call with no arguments creates a SUMMON node without a function identifier node.
    /// </summary>
    [Fact]
    public void Build_WithSummonNoArguments_CreatesSummonNodeWithoutFunctionIdentifierNode()
    {
        PrefixExpressionNode summon = new()
        {
            Operator = Operator.Summon,
            Arguments = [],
            Span = PlaceholderSpan
        };

        BlazorDiagram diagram = BuildExpression(summon);

        Node(diagram, "SummonNode");
        Nodes(diagram, "SummonFunctionNode").ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that a SUMMON call first argument becomes the function node and the rest become
    /// "Arg N" ports, numbered starting from 1.
    /// </summary>
    [Fact]
    public void Build_WithSummonWithArguments_FirstArgumentBecomesFunctionNodeRestBecomeArgPorts()
    {
        PrefixExpressionNode summon = new()
        {
            Operator = Operator.Summon,
            Arguments = [Identifier("compute"), IntegerLiteral(1), IntegerLiteral(2)],
            Span = PlaceholderSpan,
        };

        BlazorDiagram diagram = BuildExpression(summon);

        TopsyTurvyVisualNodeModel summonNode = Node(diagram, "SummonNode");
        IsLinked(Port(summonNode, "Function", VisualPortRole.DataIn)).ShouldBeTrue();
        IsLinked(Port(summonNode, "Arg 1", VisualPortRole.DataIn)).ShouldBeTrue();
        IsLinked(Port(summonNode, "Arg 2", VisualPortRole.DataIn)).ShouldBeTrue();
        Node(diagram, "SummonFunctionNode").Title.ShouldBe("compute");
    }

    /// <summary>
    /// Tests that a literal subtitle shows its type keyword and value.
    /// </summary>
    [Fact]
    public void Build_WithLiteral_SubtitleShowsTypeAndValue()
    {
        BlazorDiagram diagram = BuildExpression(IntegerLiteral(42));

        Node(diagram, "LiteralNode").Subtitle.ShouldBe("PEER : 42");
    }

    /// <summary>
    /// Tests that a null literal subtitle shows "NAUGHT" for both the type and the value.
    /// </summary>
    [Fact]
    public void Build_WithNullLiteral_SubtitleShowsNaughtForTypeAndValue()
    {
        LiteralNode nullLiteral = new()
        {
            Value = null,
            Type = LiteralType.Null,
            Span = PlaceholderSpan
        };

        BlazorDiagram diagram = BuildExpression(nullLiteral);

        Node(diagram, "LiteralNode").Subtitle.ShouldBe("NAUGHT : NAUGHT");
    }

    /// <summary>
    /// Tests that an identifier matching the enclosing function parameter name resolves to the
    /// Parameter node kind, rather than the plain Identifier kind.
    /// </summary>
    [Fact]
    public void Build_WithIdentifierMatchingCurrentFunctionParameter_UsesParameterKind()
    {
        FunctionDefinitionNode function = new()
        {
            Name = "f",
            NameSpan = PlaceholderSpan,
            Parameters = [new TypedParameter("x", LiteralType.Integer, PlaceholderSpan)],
            Body = [new PrintNode { Expression = Identifier("x"), Span = PlaceholderSpan }],
            Span = PlaceholderSpan,
        };

        BlazorDiagram diagram = Build(WrapInProgram(function));

        Nodes(diagram, "IdentifierNode").Single(node => node.SymbolIdentifierNodeName == "x").Kind.ShouldBe(VisualNodeKind.Parameter);
    }

    /// <summary>
    /// Tests that an identifier outside any function body resolves to the plain Identifier node kind.
    /// </summary>
    [Fact]
    public void Build_WithIdentifierOutsideFunctionBody_UsesIdentifierKind()
    {
        BlazorDiagram diagram = BuildExpression(Identifier("x"));

        Node(diagram, "IdentifierNode").Kind.ShouldBe(VisualNodeKind.Identifier);
    }

    /// <summary>
    /// Builds a Blazor Diagram for a given expression, wrapped in a PrintNode and a Program.
    /// </summary>
    /// <param name="expression">The expression to wrap and build.</param>
    /// <returns>A BlazorDiagram representing the built expression.</returns>
    private static BlazorDiagram BuildExpression(Expression expression) =>
        Build(WrapInProgram(new PrintNode { Expression = expression, Span = PlaceholderSpan }));
}
