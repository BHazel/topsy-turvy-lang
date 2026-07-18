using Blazor.Diagrams;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.WebEditor.Visual;

namespace BWHazel.TopsyTurvy.WebEditor.Tests.Visual;

/// <summary>
/// Declaration and assignment tests for the <see cref="VisualGraphBuilder"/> class.
/// </summary>
public class VisualGraphBuilderDeclarationAssignmentTests : VisualGraphBuilderTestBase
{
    /// <summary>
    /// Tests that a declaration with an initial value wires a linked value Data In port.
    /// </summary>
    [Fact]
    public void Build_WithDeclaration_WiresValuePortToInitialValueExpression()
    {
        DeclarationNode declaration = new()
        {
            Name = "x",
            NameSpan = PlaceholderSpan,
            Type = LiteralType.Integer,
            InitialValue = IntegerLiteral(42),
            Span = PlaceholderSpan,
        };

        BlazorDiagram diagram = Build(WrapInProgram(declaration));

        TopsyTurvyVisualNodeModel node = Node(diagram, "DeclarationNode");
        IsLinked(Port(node, "Value", VisualPortRole.DataIn)).ShouldBeTrue();
    }

    /// <summary>
    /// Tests that a constant declarationsubtitle includes the "CONSERVATIVE " prefix before the type.
    /// </summary>
    [Fact]
    public void Build_WithConstantDeclaration_SubtitleIncludesConservativePrefix()
    {
        DeclarationNode declaration = new()
        {
            Name = "x",
            NameSpan = PlaceholderSpan,
            Type = LiteralType.Integer,
            IsConstant = true,
            Span = PlaceholderSpan,
        };

        BlazorDiagram diagram = Build(WrapInProgram(declaration));

        Node(diagram, "DeclarationNode").Subtitle.ShouldBe("x : CONSERVATIVE PEER");
    }

    /// <summary>
    /// Tests that a declaration with no initial value has no "Value" Data In port at all.
    /// </summary>
    [Fact]
    public void Build_WithDeclarationWithoutInitialValue_HasNoValuePort()
    {
        DeclarationNode declaration = new()
        {
            Name = "x",
            NameSpan = PlaceholderSpan,
            Type = LiteralType.Integer,
            Span = PlaceholderSpan,
        };

        BlazorDiagram diagram = Build(WrapInProgram(declaration));

        Port(Node(diagram, "DeclarationNode"), "Value", VisualPortRole.DataIn).ShouldBeNull();
    }

    /// <summary>
    /// Tests that an assignment wires both its Variable and Value Data In ports.
    /// </summary>
    [Fact]
    public void Build_WithAssignment_WiresVariableAndValuePorts()
    {
        AssignmentNode assignment = new()
        {
            Target = "x",
            Value = IntegerLiteral(7),
            Span = PlaceholderSpan,
        };

        BlazorDiagram diagram = Build(WrapInProgram(assignment));

        TopsyTurvyVisualNodeModel node = Node(diagram, "AssignmentNode");
        IsLinked(Port(node, "Variable", VisualPortRole.DataIn)).ShouldBeTrue();
        IsLinked(Port(node, "Value", VisualPortRole.DataIn)).ShouldBeTrue();
    }

    /// <summary>
    /// Tests that an array element assignment wires its Variable, Victim and Value Data In ports.
    /// </summary>
    [Fact]
    public void Build_WithArrayElementAssignment_WiresVariableVictimAndValuePorts()
    {
        ArrayElementAssignmentNode assignment = new()
        {
            ArrayName = "items",
            Index = IntegerLiteral(0),
            Value = IntegerLiteral(7),
            Span = PlaceholderSpan,
        };

        BlazorDiagram diagram = Build(WrapInProgram(assignment));

        TopsyTurvyVisualNodeModel node = Node(diagram, "ArrayElementAssignmentNode");
        IsLinked(Port(node, "Variable", VisualPortRole.DataIn)).ShouldBeTrue();
        IsLinked(Port(node, "Victim", VisualPortRole.DataIn)).ShouldBeTrue();
        IsLinked(Port(node, "Value", VisualPortRole.DataIn)).ShouldBeTrue();
    }

    /// <summary>
    /// Tests that an input statement wires its "Variable" Data In port to a target identifier node.
    /// </summary>
    [Fact]
    public void Build_WithInput_WiresVariablePortToTargetIdentifier()
    {
        InputNode input = new() { Target = "x", Span = PlaceholderSpan };

        BlazorDiagram diagram = Build(WrapInProgram(input));

        TopsyTurvyVisualNodeModel node = Node(diagram, "InputNode");
        IsLinked(Port(node, "Variable", VisualPortRole.DataIn)).ShouldBeTrue();
    }

    /// <summary>
    /// Tests that a pointer declaration with an initial value wires a linked value Data In port.
    /// </summary>
    [Fact]
    public void Build_WithPointerDeclaration_WiresValuePortToInitialValueExpression()
    {
        PointerDeclarationNode declaration = new()
        {
            Name = "NumberPointer",
            NameSpan = PlaceholderSpan,
            PointeeType = LiteralType.Integer,
            InitialValue = new AddressOfExpressionNode { VariableName = "Number", Span = PlaceholderSpan },
            Span = PlaceholderSpan,
        };

        BlazorDiagram diagram = Build(WrapInProgram(declaration));

        TopsyTurvyVisualNodeModel node = Node(diagram, "PointerDeclarationNode");
        IsLinked(Port(node, "Value", VisualPortRole.DataIn)).ShouldBeTrue();
    }

    /// <summary>
    /// Tests that a pointer declaration subtitle includes the pointee type after the GALLERY PICTURE OF keyword.
    /// </summary>
    [Fact]
    public void Build_WithPointerDeclaration_SubtitleIncludesPointeeType()
    {
        PointerDeclarationNode declaration = new()
        {
            Name = "NumberPointer",
            NameSpan = PlaceholderSpan,
            PointeeType = LiteralType.Integer,
            Span = PlaceholderSpan,
        };

        BlazorDiagram diagram = Build(WrapInProgram(declaration));

        Node(diagram, "PointerDeclarationNode").Subtitle.ShouldBe("NumberPointer : GALLERY PICTURE OF PEER");
    }

    /// <summary>
    /// Tests that a constant pointer declaration subtitle includes the "CONSERVATIVE " prefix before the pointee type.
    /// </summary>
    [Fact]
    public void Build_WithConstantPointerDeclaration_SubtitleIncludesConservativePrefix()
    {
        PointerDeclarationNode declaration = new()
        {
            Name = "NumberPointer",
            NameSpan = PlaceholderSpan,
            PointeeType = LiteralType.Integer,
            IsConstant = true,
            Span = PlaceholderSpan,
        };

        BlazorDiagram diagram = Build(WrapInProgram(declaration));

        Node(diagram, "PointerDeclarationNode").Subtitle.ShouldBe("NumberPointer : CONSERVATIVE GALLERY PICTURE OF PEER");
    }

    /// <summary>
    /// Tests that a pointer declaration with no initial value has no "Value" Data In port at all.
    /// </summary>
    [Fact]
    public void Build_WithPointerDeclarationWithoutInitialValue_HasNoValuePort()
    {
        PointerDeclarationNode declaration = new()
        {
            Name = "NumberPointer",
            NameSpan = PlaceholderSpan,
            PointeeType = LiteralType.Integer,
            Span = PlaceholderSpan,
        };

        BlazorDiagram diagram = Build(WrapInProgram(declaration));

        Port(Node(diagram, "PointerDeclarationNode"), "Value", VisualPortRole.DataIn).ShouldBeNull();
    }

    /// <summary>
    /// Tests that a dereference assignment wires its Pointer and Value Data In ports.
    /// </summary>
    [Fact]
    public void Build_WithDereferenceAssignment_WiresPointerAndValuePorts()
    {
        DereferenceAssignmentNode assignment = new()
        {
            PointerName = "NumberPointer",
            Value = IntegerLiteral(23),
            Span = PlaceholderSpan,
        };

        BlazorDiagram diagram = Build(WrapInProgram(assignment));

        TopsyTurvyVisualNodeModel node = Node(diagram, "DereferenceAssignmentNode");
        IsLinked(Port(node, "Pointer", VisualPortRole.DataIn)).ShouldBeTrue();
        IsLinked(Port(node, "Value", VisualPortRole.DataIn)).ShouldBeTrue();
    }
}
