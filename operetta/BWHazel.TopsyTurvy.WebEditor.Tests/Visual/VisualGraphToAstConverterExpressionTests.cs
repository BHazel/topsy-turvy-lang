using System.Linq;
using Blazor.Diagrams;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using Blazor.Diagrams.Core.Models.Base;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.WebEditor.Visual;

namespace BWHazel.TopsyTurvy.WebEditor.Tests.Visual;

/// <summary>
/// Expression round-trip tests for the <see cref="VisualGraphToAstConverter"/> class.
/// </summary>
public class VisualGraphToAstConverterExpressionTests : VisualGraphToAstConverterTestBase
{
    /// <summary>
    /// Tests that a literal round-trips with its type and value preserved, for the numeric, string and
    /// character literal types.
    /// </summary>
    /// <param name="type">The literal type to test.</param>
    /// <param name="value">The literal value to test.</param>
    [Theory]
    [InlineData(LiteralType.Integer, 42)]
    [InlineData(LiteralType.Long, 42L)]
    [InlineData(LiteralType.Short, (short)42)]
    [InlineData(LiteralType.SignedByte, (sbyte)42)]
    [InlineData(LiteralType.UnsignedInteger, 42u)]
    [InlineData(LiteralType.UnsignedLong, 42ul)]
    [InlineData(LiteralType.UnsignedShort, (ushort)42)]
    [InlineData(LiteralType.Byte, (byte)42)]
    [InlineData(LiteralType.Double, 3.14)]
    [InlineData(LiteralType.Single, 3.14f)]
    [InlineData(LiteralType.String, "hello")]
    [InlineData(LiteralType.Char, 'A')]
    public void RoundTrip_Literal_NumericStringOrCharType_PreservesTypeAndValue(LiteralType type, object value)
    {
        LiteralNode literal = new()
        {
            Value = value,
            Type = type,
            Span = PlaceholderSpan
        };

        LiteralNode result = (LiteralNode)ExtractExpression(RoundTripExpression(literal));

        result.Type.ShouldBe(type);
        result.Value.ShouldBe(value);
    }

    /// <summary>
    /// Tests that a null literal value stays null after round-tripping.
    /// </summary>
    [Fact]
    public void RoundTrip_NullLiteral_ValueStaysNull()
    {
        LiteralNode literal = new()
        {
            Value = null,
            Type = LiteralType.Null,
            Span = PlaceholderSpan
        };

        LiteralNode result = (LiteralNode)ExtractExpression(RoundTripExpression(literal));

        result.Value.ShouldBeNull();
    }

    /// <summary>
    /// Tests that a boolean literal raw string value is parsed case-insensitively, accepting "VERITY" and
    /// "True", and treating anything else as false.
    /// </summary>
    /// <param name="rawValue">The raw string value to test.</param>
    /// <param name="expected">The expected boolean value after parsing.</param>
    [Theory]
    [InlineData("VERITY", true)]
    [InlineData("True", true)]
    [InlineData("nonsense", false)]
    public void RoundTrip_BooleanLiteral_AcceptsVerityAndTrueCaseInsensitively_OthersAreFalse(string rawValue, bool expected)
    {
        LiteralNode literal = new()
        {
            Value = true,
            Type = LiteralType.Boolean,
            Span = PlaceholderSpan
        };

        BlazorDiagram diagram = Build(WrapInProgram(new PrintNode() { Expression = literal, Span = PlaceholderSpan }));
        diagram.Nodes.OfType<TopsyTurvyVisualNodeModel>().Single(node => node.StatementType == "LiteralNode").LiteralValue = rawValue;

        ProgramNode reconstructed = Convert(diagram);

        LiteralNode result = (LiteralNode)ExtractExpression(reconstructed);
        result.Value.ShouldBe(expected);
    }

    /// <summary>
    /// Tests that an identifier round-trips with its name preserved.
    /// </summary>
    [Fact]
    public void RoundTrip_Identifier_PreservesName()
    {
        IdentifierNode identifier = new()
        {
            Name = "x",
            Span = PlaceholderSpan
        };

        IdentifierNode result = (IdentifierNode)ExtractExpression(RoundTripExpression(identifier));

        result.Name.ShouldBe("x");
    }

    /// <summary>
    /// Tests that each operator family round-trips with its operator and argument order preserved.
    /// </summary>
    /// <param name="prefixOperator">The operator to test.</param>
    [Theory]
    [InlineData(Operator.Sum)]
    [InlineData(Operator.Both)]
    [InlineData(Operator.AllOf)]
    [InlineData(Operator.WovenOf)]
    public void RoundTrip_OperatorExpression_PreservesOperatorAndArgumentOrder(Operator prefixOperator)
    {
        PrefixExpressionNode prefix = new()
        {
            Operator = prefixOperator,
            Arguments = [
                new LiteralNode() { Value = 1, Type = LiteralType.Integer, Span = PlaceholderSpan },
                new LiteralNode() { Value = 2, Type = LiteralType.Integer, Span = PlaceholderSpan },
            ],
            Span = PlaceholderSpan,
        };

        PrefixExpressionNode result = (PrefixExpressionNode)ExtractExpression(RoundTripExpression(prefix));

        result.Operator.ShouldBe(prefixOperator);
        result.Arguments.Count.ShouldBe(2);
        ((LiteralNode)result.Arguments[0]).Value.ShouldBe(1);
        ((LiteralNode)result.Arguments[1]).Value.ShouldBe(2);
    }

    /// <summary>
    /// Tests that a ternary expression round-trips with its condition, true and false values preserved.
    /// </summary>
    [Fact]
    public void RoundTrip_Ternary_PreservesConditionTrueAndFalseValues()
    {
        TernaryExpressionNode ternary = new()
        {
            Condition = new LiteralNode { Value = true, Type = LiteralType.Boolean, Span = PlaceholderSpan },
            TrueValue = new LiteralNode { Value = 1, Type = LiteralType.Integer, Span = PlaceholderSpan },
            FalseValue = new LiteralNode { Value = 2, Type = LiteralType.Integer, Span = PlaceholderSpan },
            Span = PlaceholderSpan,
        };

        TernaryExpressionNode result = (TernaryExpressionNode)ExtractExpression(RoundTripExpression(ternary));

        ((LiteralNode)result.TrueValue).Value.ShouldBe(1);
        ((LiteralNode)result.FalseValue).Value.ShouldBe(2);
    }

    /// <summary>
    /// Tests that an array index expression round-trips with its array name and index expression preserved.
    /// </summary>
    [Fact]
    public void RoundTrip_ArrayIndex_PreservesArrayNameAndIndexExpression()
    {
        ArrayIndexNode arrayIndex = new()
        {
            ArrayName = "items",
            Index = new LiteralNode() { Value = 0, Type = LiteralType.Integer, Span = PlaceholderSpan },
            Span = PlaceholderSpan
        };

        ArrayIndexNode result = (ArrayIndexNode)ExtractExpression(RoundTripExpression(arrayIndex));

        result.ArrayName.ShouldBe("items");
        ((LiteralNode)result.Index).Value.ShouldBe(0);
    }

    /// <summary>
    /// Tests that an array length expression round-trips with its array name preserved.
    /// </summary>
    [Fact]
    public void RoundTrip_ArrayLength_PreservesArrayName()
    {
        ArrayLengthNode arrayLength = new()
        {
            ArrayName = "items",
            Span = PlaceholderSpan
        };

        ArrayLengthNode result = (ArrayLengthNode)ExtractExpression(RoundTripExpression(arrayLength));

        result.ArrayName.ShouldBe("items");
    }

    /// <summary>
    /// Tests that an address-of expression round-trips with its target variable name preserved.
    /// </summary>
    [Fact]
    public void RoundTrip_AddressOf_PreservesVariableName()
    {
        AddressOfExpressionNode addressOf = new()
        {
            VariableName = "Number",
            Span = PlaceholderSpan
        };

        AddressOfExpressionNode result = (AddressOfExpressionNode)ExtractExpression(RoundTripExpression(addressOf));

        result.VariableName.ShouldBe("Number");
    }

    /// <summary>
    /// Tests that a dereference expression round-trips with its pointer name preserved.
    /// </summary>
    [Fact]
    public void RoundTrip_Dereference_PreservesPointerName()
    {
        DereferenceExpressionNode dereference = new()
        {
            PointerName = "NumberPointer",
            Span = PlaceholderSpan
        };

        DereferenceExpressionNode result = (DereferenceExpressionNode)ExtractExpression(RoundTripExpression(dereference));

        result.PointerName.ShouldBe("NumberPointer");
    }

    /// <summary>
    /// Tests that an expression cast round-trips with its inner expression and target type preserved.
    /// </summary>
    [Fact]
    public void RoundTrip_ExpressionCast_PreservesInnerExpressionAndNewType()
    {
        ExpressionCastNode cast = new()
        {
            Expression = new LiteralNode() { Value = 1, Type = LiteralType.Integer, Span = PlaceholderSpan },
            NewType = LiteralType.Double,
            Span = PlaceholderSpan
        };

        ExpressionCastNode result = (ExpressionCastNode)ExtractExpression(RoundTripExpression(cast));

        result.NewType.ShouldBe(LiteralType.Double);
        ((LiteralNode)result.Expression).Value.ShouldBe(1);
    }

    /// <summary>
    /// Tests that a SUMMON call with arguments round-trips with the function name and argument order preserved.
    /// </summary>
    [Fact]
    public void RoundTrip_SummonWithArguments_PreservesFunctionNameAndArgumentOrder()
    {
        PrefixExpressionNode summon = new()
        {
            Operator = Operator.Summon,
            Arguments = [
                new IdentifierNode { Name = "compute", Span = PlaceholderSpan },
                new LiteralNode { Value = 1, Type = LiteralType.Integer, Span = PlaceholderSpan },
            ],
            Span = PlaceholderSpan,
        };

        PrefixExpressionNode result = (PrefixExpressionNode)ExtractExpression(RoundTripExpression(summon));

        result.Operator.ShouldBe(Operator.Summon);
        result.Arguments.Count.ShouldBe(2);
        ((IdentifierNode)result.Arguments[0]).Name.ShouldBe("compute");
        ((LiteralNode)result.Arguments[1]).Value.ShouldBe(1);
    }

    /// <summary>
    /// Tests that a SUMMON call reconstructs its arguments, in declaration order, when they are wired to
    /// the function-reference node's own parameter-named ports rather than to "Arg N" ports on the SUMMON
    /// node itself.
    /// </summary>
    [Fact]
    public void Factory_SummonWithArgumentsWiredToFunctionReferenceNodeParameterPorts_PreservesDeclarationOrder()
    {
        PrintNode print = new() { Expression = new LiteralNode { Value = 0, Type = LiteralType.Integer, Span = PlaceholderSpan }, Span = PlaceholderSpan };
        BlazorDiagram diagram = Build(WrapInProgram(print));
        TopsyTurvyVisualNodeModel printNode = diagram.Nodes.OfType<TopsyTurvyVisualNodeModel>().Single(node => node.StatementType == "PrintNode");
        TopsyTurvyVisualPortModel exprPort = printNode.Ports.OfType<TopsyTurvyVisualPortModel>().Single(port => port.Label == "Expr");
        foreach (BaseLinkModel staleLink in exprPort.Links.ToList())
        {
            diagram.Links.Remove(staleLink);
        }

        int counter = 0;
        TopsyTurvyVisualNodeModel summonNode = VisualNodeFactory.CreateExpression("SummonNode", new Point(0, 0), diagram, ref counter)!;
        TopsyTurvyVisualNodeModel functionReference = VisualNodeFactory.CreateExpression("FunctionReferenceNode", new Point(0, 0), diagram, ref counter)!;
        functionReference.SymbolIdentifierNodeName = "Add";
        functionReference.Title = "Add";

        TopsyTurvyVisualPortModel betaPort = new(functionReference, PortAlignment.Left, "Beta", VisualPortRole.DataIn);
        functionReference.AddPort(betaPort);
        TopsyTurvyVisualPortModel alphaPort = new(functionReference, PortAlignment.Left, "Alpha", VisualPortRole.DataIn);
        functionReference.AddPort(alphaPort);

        TopsyTurvyVisualPortModel functionPort = summonNode.Ports.OfType<TopsyTurvyVisualPortModel>().Single(port => port.Label == "Function");
        TopsyTurvyVisualPortModel functionOutPort = functionReference.Ports.OfType<TopsyTurvyVisualPortModel>().Single(port => port.Role == VisualPortRole.DataOut);
        diagram.Links.Add(new LinkModel(functionOutPort, functionPort));

        TopsyTurvyVisualNodeModel firstLiteral = MakeLiteralNode(diagram, 4);
        diagram.Links.Add(new LinkModel(firstLiteral.Ports.OfType<TopsyTurvyVisualPortModel>().Single(port => port.Role == VisualPortRole.DataOut), betaPort));

        TopsyTurvyVisualNodeModel secondLiteral = MakeLiteralNode(diagram, 5);
        diagram.Links.Add(new LinkModel(secondLiteral.Ports.OfType<TopsyTurvyVisualPortModel>().Single(port => port.Role == VisualPortRole.DataOut), alphaPort));

        TopsyTurvyVisualPortModel summonOutPort = summonNode.Ports.OfType<TopsyTurvyVisualPortModel>().Single(port => port.Role == VisualPortRole.DataOut);
        diagram.Links.Add(new LinkModel(summonOutPort, exprPort));

        ProgramNode reconstructed = Convert(diagram);

        PrefixExpressionNode result = (PrefixExpressionNode)ExtractExpression(reconstructed);
        result.Operator.ShouldBe(Operator.Summon);
        result.Arguments.Count.ShouldBe(3);
        ((IdentifierNode)result.Arguments[0]).Name.ShouldBe("Add");
        ((LiteralNode)result.Arguments[1]).Value.ShouldBe(4);
        ((LiteralNode)result.Arguments[2]).Value.ShouldBe(5);
    }

    /// <summary>
    /// Tests that an operator node whose title does not map to any known operator falls back to <see cref="Operator.Sum"/>.
    /// </summary>
    [Fact]
    public void Factory_OperatorNodeWithUnmappedTitle_FallsBackToSumOperator()
    {
        PrefixExpressionNode prefix = new()
        {
            Operator = Operator.ChordOf,
            Arguments = [
                new LiteralNode { Value = 1, Type = LiteralType.Integer, Span = PlaceholderSpan },
                new LiteralNode { Value = 2, Type = LiteralType.Integer, Span = PlaceholderSpan },
            ],
            Span = PlaceholderSpan,
        };
        
        BlazorDiagram diagram = Build(WrapInProgram(new PrintNode { Expression = prefix, Span = PlaceholderSpan }));
        diagram.Nodes.OfType<TopsyTurvyVisualNodeModel>().Single(node => node.StatementType == "BitwiseNode").Title = "NOT A REAL OPERATOR";

        ProgramNode reconstructed = Convert(diagram);

        ((PrefixExpressionNode)ExtractExpression(reconstructed)).Operator.ShouldBe(Operator.Sum);
    }

    /// <summary>
    /// Tests that an expression node with an unrecognised statement type and no original AST link falls back
    /// to a null literal, rather than throwing.
    /// </summary>
    [Fact]
    public void Factory_ExpressionNodeWithUnknownStatementTypeAndNoAst_FallsBackToNullLiteral()
    {
        LiteralNode literal = new() { Value = 1, Type = LiteralType.Integer, Span = PlaceholderSpan };
        BlazorDiagram diagram = Build(WrapInProgram(new PrintNode { Expression = literal, Span = PlaceholderSpan }));
        TopsyTurvyVisualNodeModel literalNode = diagram.Nodes.OfType<TopsyTurvyVisualNodeModel>().Single(node => node.StatementType == "LiteralNode");
        literalNode.StatementType = "NotARealExpressionType";
        literalNode.AstNode = null;

        ProgramNode reconstructed = Convert(diagram);

        LiteralNode result = (LiteralNode)ExtractExpression(reconstructed);
        result.Type.ShouldBe(LiteralType.Null);
        result.Value.ShouldBeNull();
    }

    /// <summary>
    /// Creates a literal node with the given integer value and adds it to the diagram.
    /// </summary>
    /// <param name="diagram">The diagram to which the literal node will be added.</param>
    /// <param name="value">The integer value of the literal node.</param>
    /// <returns>The created literal node.</returns>
    private static TopsyTurvyVisualNodeModel MakeLiteralNode(BlazorDiagram diagram, int value)
    {
        TopsyTurvyVisualNodeModel node = new($"literal-{value}", new Point(0, 0), "Literal", null, VisualNodeKind.Literal)
        {
            StatementType = "LiteralNode",
            LiteralValue = value.ToString(),
            NodeLiteralType = LiteralType.Integer,
        };
        node.AddPort(new TopsyTurvyVisualPortModel(node, PortAlignment.Right, "Out", VisualPortRole.DataOut));
        diagram.Nodes.Add(node);
        return node;
    }
}
