using System.Linq;
using Blazor.Diagrams;
using Blazor.Diagrams.Core.Geometry;
using Blazor.Diagrams.Core.Models;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.WebEditor.Visual;

namespace BWHazel.TopsyTurvy.WebEditor.Tests.Visual;

/// <summary>
/// Declaration and assignment round-trip tests for the <see cref="VisualGraphToAstConverter"/> class.
/// </summary>
public class VisualGraphToAstConverterDeclarationAssignmentTests : VisualGraphToAstConverterTestBase
{
    /// <summary>
    /// Tests that a declaration round-trips with its initial value and constant flag preserved.
    /// </summary>
    [Fact]
    public void RoundTrip_Declaration_PreservesInitialValueAndConstantFlag()
    {
        DeclarationNode declaration = new()
        {
            Name = "x",
            NameSpan = PlaceholderSpan,
            Type = LiteralType.Integer,
            IsConstant = true,
            InitialValue = new LiteralNode() { Value = 42, Type = LiteralType.Integer, Span = PlaceholderSpan },
            Span = PlaceholderSpan,
        };

        ProgramNode reconstructed = RoundTrip(WrapInProgram(declaration));

        DeclarationNode result = reconstructed.Statements.OfType<DeclarationNode>().Single();
        result.Name.ShouldBe("x");
        result.IsConstant.ShouldBeTrue();
        ((LiteralNode)result.InitialValue!).Value.ShouldBe(42);
    }

    /// <summary>
    /// Tests that an array declaration with initial values, reconstructed via the AST-backed path,
    /// preserves the element order.
    /// </summary>
    [Fact]
    public void RoundTrip_ArrayDeclarationWithInitialValues_PreservesElementOrder()
    {
        ArrayDeclarationNode declaration = new()
        {
            Name = "items",
            NameSpan = PlaceholderSpan,
            ElementType = LiteralType.Integer,
            InitialValues = [
                new LiteralNode() { Value = 1, Type = LiteralType.Integer, Span = PlaceholderSpan },
                new LiteralNode() { Value = 2, Type = LiteralType.Integer, Span = PlaceholderSpan },
            ],
            Span = PlaceholderSpan,
        };

        ProgramNode reconstructed = RoundTrip(WrapInProgram(declaration));

        ArrayDeclarationNode result = reconstructed.Statements.OfType<ArrayDeclarationNode>().Single();
        result.InitialValues.Count.ShouldBe(2);
        ((LiteralNode)result.InitialValues[0]).Value.ShouldBe(1);
        ((LiteralNode)result.InitialValues[1]).Value.ShouldBe(2);
    }

    /// <summary>
    /// Tests that an array declaration with no initial values but a declared size, reconstructed via the
    /// AST-backed path, preserves the size with an empty initial-values list.
    /// </summary>
    [Fact]
    public void RoundTrip_ArrayDeclarationSizeOnly_PreservesSizeWithEmptyInitialValues()
    {
        ArrayDeclarationNode declaration = new()
        {
            Name = "items",
            NameSpan = PlaceholderSpan,
            ElementType = LiteralType.Integer,
            SizeExpression = IntegerLiteral(5),
            InitialValues = [],
            Span = PlaceholderSpan,
        };

        ProgramNode reconstructed = RoundTrip(WrapInProgram(declaration));

        ArrayDeclarationNode result = reconstructed.Statements.OfType<ArrayDeclarationNode>().Single();
        ((LiteralNode)result.SizeExpression!).Value.ShouldBe(5);
        result.InitialValues.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that an assignment and an array element assignment round-trip with their targets and values preserved.
    /// </summary>
    [Fact]
    public void RoundTrip_AssignmentAndArrayElementAssignment_PreserveTargetsAndValues()
    {
        AssignmentNode assignment = new() { Target = "x", Value = new LiteralNode { Value = 7, Type = LiteralType.Integer, Span = PlaceholderSpan }, Span = PlaceholderSpan };
        ArrayElementAssignmentNode arrayAssignment = new()
        {
            ArrayName = "items",
            Index = new LiteralNode { Value = 0, Type = LiteralType.Integer, Span = PlaceholderSpan },
            Value = new LiteralNode { Value = 9, Type = LiteralType.Integer, Span = PlaceholderSpan },
            Span = PlaceholderSpan,
        };

        ProgramNode reconstructed = RoundTrip(WrapInProgram(assignment, arrayAssignment));

        AssignmentNode assignmentResult = reconstructed.Statements.OfType<AssignmentNode>().Single();
        assignmentResult.Target.ShouldBe("x");
        ((LiteralNode)assignmentResult.Value).Value.ShouldBe(7);
        ArrayElementAssignmentNode arrayResult = reconstructed.Statements.OfType<ArrayElementAssignmentNode>().Single();
        arrayResult.ArrayName.ShouldBe("items");
        ((LiteralNode)arrayResult.Value).Value.ShouldBe(9);
    }

    /// <summary>
    /// Tests that an input statement round-trips with its target preserved.
    /// </summary>
    [Fact]
    public void RoundTrip_Input_PreservesTarget()
    {
        InputNode input = new() { Target = "x", Span = PlaceholderSpan };

        ProgramNode reconstructed = RoundTrip(WrapInProgram(input));

        reconstructed.Statements.OfType<InputNode>().Single().Target.ShouldBe("x");
    }

    /// <summary>
    /// Tests that a declaration node with no original AST link is reconstructed from the visual node
    /// own properties rather than crashing or falling back to a default.
    /// </summary>
    [Fact]
    public void Factory_Declaration_ReconstructsFromVisualNodePropertiesNotAst()
    {
        DeclarationNode declaration = new() { Name = "x", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan };

        ProgramNode reconstructed = RoundTripAsFactory(WrapInProgram(declaration), "DeclarationNode");

        reconstructed.Statements.OfType<DeclarationNode>().Single().Name.ShouldBe("x");
    }

    /// <summary>
    /// Tests that a fresh array declaration with no wired Victim ports and no size (the shape produced when the
    /// node was created directly as an array declaration with no elements) is reconstructed via the factory path
    /// with an empty initial-values list and no size.
    /// </summary>
    [Fact]
    public void Factory_ArrayDeclarationWithNoWiredPorts_UsesStaticFactoryPath()
    {
        ArrayDeclarationNode declaration = new() { Name = "items", NameSpan = PlaceholderSpan, ElementType = LiteralType.Integer, InitialValues = [], Span = PlaceholderSpan };

        ProgramNode reconstructed = RoundTripAsFactory(WrapInProgram(declaration), "ArrayDeclarationNode");

        ArrayDeclarationNode result = reconstructed.Statements.OfType<ArrayDeclarationNode>().Single();
        result.Name.ShouldBe("items");
        result.InitialValues.ShouldBeEmpty();
        result.SizeExpression.ShouldBeNull();
    }

    /// <summary>
    /// Tests that a fresh array declaration with a wired "Size" Data In port is reconstructed via the factory
    /// path by reading the linked expression node from the diagram, not a plain-text field.
    /// </summary>
    [Fact]
    public void Factory_ArrayDeclarationWithWiredSizePort_ReadsSizeFromDiagram()
    {
        ArrayDeclarationNode declaration = new()
        {
            Name = "items",
            NameSpan = PlaceholderSpan,
            ElementType = LiteralType.Integer,
            SizeExpression = IntegerLiteral(5),
            InitialValues = [],
            Span = PlaceholderSpan
        };

        ProgramNode reconstructed = RoundTripAsFactory(WrapInProgram(declaration), "ArrayDeclarationNode");

        ArrayDeclarationNode result = reconstructed.Statements.OfType<ArrayDeclarationNode>().Single();
        ((LiteralNode)result.SizeExpression!).Value.ShouldBe(5);
    }

    /// <summary>
    /// Tests that a node originally built as a scalar declaration (StatementType "DeclarationNode"), whose
    /// literal type was later switched to Array in the editor and given wired "Victim N" ports, is
    /// reconstructed via <c>ReconstructArrayDeclarationFromFactory</c>.
    /// </summary>
    /// <remarks>
    ///  This is the one path of the three array declaration reconstruction paths that reads elements from
    /// the diagram wired ports while keyed on the "DeclarationNode" statement type rather than "ArrayDeclarationNode".
    /// </remarks>
    [Fact]
    public void Factory_ArrayDeclarationTypeSwitchedFromScalar_UsesArrayDeclarationFromFactoryPath()
    {
        DeclarationNode scalar = new() { Name = "items", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan };
        BlazorDiagram diagram = Build(WrapInProgram(scalar));

        TopsyTurvyVisualNodeModel node = diagram.Nodes.OfType<TopsyTurvyVisualNodeModel>().Single(n => n.StatementType == "DeclarationNode");
        node.AstNode = null;
        node.NodeLiteralType = LiteralType.Array;
        node.ArrayElementLiteralType = LiteralType.Integer;

        TopsyTurvyVisualPortModel victimPort = new(node, PortAlignment.Left, "Victim 1", VisualPortRole.DataIn);
        node.AddPort(victimPort);

        TopsyTurvyVisualNodeModel literal = new("literal-1", new Point(0, 0), "Literal", "PEER : 5", VisualNodeKind.Literal)
        {
            StatementType = "LiteralNode",
            LiteralValue = "5",
            NodeLiteralType = LiteralType.Integer,
        };
        TopsyTurvyVisualPortModel literalOutPort = new(literal, PortAlignment.Right, "Out", VisualPortRole.DataOut);
        literal.AddPort(literalOutPort);
        diagram.Nodes.Add(literal);
        diagram.Links.Add(new LinkModel(literalOutPort, victimPort));

        ProgramNode reconstructed = Convert(diagram);

        ArrayDeclarationNode result = reconstructed.Statements.OfType<ArrayDeclarationNode>().Single();
        result.Name.ShouldBe("items");
        result.ElementType.ShouldBe(LiteralType.Integer);
        result.InitialValues.Count.ShouldBe(1);
        ((LiteralNode)result.InitialValues[0]).Value.ShouldBe(5);
    }

    /// <summary>
    /// Tests that a pointer declaration round-trips with its pointee type, constant flag and initial value preserved.
    /// </summary>
    [Fact]
    public void RoundTrip_PointerDeclaration_PreservesPointeeTypeConstantFlagAndInitialValue()
    {
        PointerDeclarationNode declaration = new()
        {
            Name = "NumberPointer",
            NameSpan = PlaceholderSpan,
            PointeeType = LiteralType.Integer,
            IsConstant = true,
            InitialValue = new AddressOfExpressionNode { VariableName = "Number", Span = PlaceholderSpan },
            Span = PlaceholderSpan,
        };

        ProgramNode reconstructed = RoundTrip(WrapInProgram(declaration));

        PointerDeclarationNode result = reconstructed.Statements.OfType<PointerDeclarationNode>().Single();
        result.Name.ShouldBe("NumberPointer");
        result.PointeeType.ShouldBe(LiteralType.Integer);
        result.IsConstant.ShouldBeTrue();
        ((AddressOfExpressionNode)result.InitialValue!).VariableName.ShouldBe("Number");
    }

    /// <summary>
    /// Tests that a dereference assignment round-trips with its pointer name and value preserved.
    /// </summary>
    [Fact]
    public void RoundTrip_DereferenceAssignment_PreservesPointerNameAndValue()
    {
        DereferenceAssignmentNode assignment = new()
        {
            PointerName = "NumberPointer",
            Value = new LiteralNode { Value = 23, Type = LiteralType.Integer, Span = PlaceholderSpan },
            Span = PlaceholderSpan,
        };

        ProgramNode reconstructed = RoundTrip(WrapInProgram(assignment));

        DereferenceAssignmentNode result = reconstructed.Statements.OfType<DereferenceAssignmentNode>().Single();
        result.PointerName.ShouldBe("NumberPointer");
        ((LiteralNode)result.Value).Value.ShouldBe(23);
    }

    /// <summary>
    /// Tests that a node originally built as a scalar declaration (StatementType "DeclarationNode"), whose
    /// literal type was later switched to Pointer in the editor, is reconstructed via
    /// <c>ReconstructPointerDeclarationFactory</c>, matching the deliberate no-dedicated-menu-entry design where
    /// pointers are created by promoting the generic PRAY WELCOME node Type dropdown.
    /// </summary>
    [Fact]
    public void Factory_PointerDeclarationTypeSwitchedFromScalar_UsesPointerDeclarationFromFactoryPath()
    {
        DeclarationNode scalar = new() { Name = "NumberPointer", NameSpan = PlaceholderSpan, Type = LiteralType.Integer, Span = PlaceholderSpan };
        BlazorDiagram diagram = Build(WrapInProgram(scalar));

        TopsyTurvyVisualNodeModel node = diagram.Nodes.OfType<TopsyTurvyVisualNodeModel>().Single(n => n.StatementType == "DeclarationNode");
        node.AstNode = null;
        node.NodeLiteralType = LiteralType.Pointer;
        node.PointerPointeeLiteralType = LiteralType.Integer;

        ProgramNode reconstructed = Convert(diagram);

        PointerDeclarationNode result = reconstructed.Statements.OfType<PointerDeclarationNode>().Single();
        result.Name.ShouldBe("NumberPointer");
        result.PointeeType.ShouldBe(LiteralType.Integer);
    }
}
