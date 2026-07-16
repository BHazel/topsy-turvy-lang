using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Parser;

namespace BWHazel.TopsyTurvy.Tests.Parser;

/// <summary>
/// Tests for pointer statement and expression forms parsed by the <see cref="TopsyTurvyParser"/> class.
/// </summary>
public class TopsyTurvyParserPointerTests
{
    private readonly TopsyTurvyParser parser = new();

    /// <summary>
    /// Tests that a pointer declaration without a BEING clause produces a <see cref="PointerDeclarationNode"/> with a null initial value.
    /// </summary>
    [Fact]
    public void Parse_PointerDeclaration_WithoutInitialValue_ParsesCorrectly()
    {
        PointerDeclarationNode node = this.ParseFirstStatement<PointerDeclarationNode>(
            "PRAY WELCOME NumberPointer AS A GALLERY PICTURE OF PEER");

        node.Name.ShouldBe("NumberPointer");
        node.PointeeType.ShouldBe(LiteralType.Integer);
        node.IsConstant.ShouldBeFalse();
        node.InitialValue.ShouldBeNull();
    }

    /// <summary>
    /// Tests that a pointer declaration with a BEING GALLERY PICTURE TO clause produces a <see cref="PointerDeclarationNode"/> with an <see cref="AddressOfExpressionNode"/> initial value.
    /// </summary>
    [Fact]
    public void Parse_PointerDeclaration_WithInitialValue_ParsesCorrectly()
    {
        PointerDeclarationNode node = this.ParseFirstStatement<PointerDeclarationNode>(
            "PRAY WELCOME NumberPointer AS A GALLERY PICTURE OF PEER BEING GALLERY PICTURE TO Number");

        node.Name.ShouldBe("NumberPointer");
        node.PointeeType.ShouldBe(LiteralType.Integer);
        AddressOfExpressionNode addressOf = node.InitialValue.ShouldBeOfType<AddressOfExpressionNode>();
        addressOf.VariableName.ShouldBe("Number");
    }

    /// <summary>
    /// Tests that a pointer declaration with the CONSERVATIVE modifier sets <see cref="PointerDeclarationNode.IsConstant"/> to <c>true</c>.
    /// </summary>
    [Fact]
    public void Parse_PointerDeclaration_WithConservativeModifier_SetsIsConstantTrue()
    {
        PointerDeclarationNode node = this.ParseFirstStatement<PointerDeclarationNode>(
            "PRAY WELCOME NumberPointer AS A CONSERVATIVE GALLERY PICTURE OF PEER");

        node.IsConstant.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that each scalar type keyword maps to the correct <see cref="LiteralType"/> on the <see cref="PointerDeclarationNode.PointeeType"/> property.
    /// </summary>
    /// <param name="keyword">The type keyword to test.</param>
    /// <param name="expectedType">The expected pointee <see cref="LiteralType"/>.</param>
    [Theory]
    [InlineData("PEER", LiteralType.Integer)]
    [InlineData("FATHOM", LiteralType.Double)]
    [InlineData("YARN", LiteralType.String)]
    [InlineData("STITCH", LiteralType.Char)]
    [InlineData("DECREE", LiteralType.Boolean)]
    public void Parse_PointerDeclaration_WithTypeKeyword_MapsToCorrectPointeeType(string keyword, LiteralType expectedType)
    {
        PointerDeclarationNode node = this.ParseFirstStatement<PointerDeclarationNode>(
            $"PRAY WELCOME ptr AS A GALLERY PICTURE OF {keyword}");

        node.PointeeType.ShouldBe(expectedType);
    }

    /// <summary>
    /// Tests that a GALLERY PICTURE TO expression used as an assignment value produces an <see cref="AddressOfExpressionNode"/> with the target variable name set correctly.
    /// </summary>
    [Fact]
    public void Parse_AddressOfExpression_ValidSyntax_ParsesCorrectly()
    {
        AssignmentNode node = this.ParseFirstStatement<AssignmentNode>(
            "NumberPointer IS APPOINTED GALLERY PICTURE TO Number");

        node.Target.ShouldBe("NumberPointer");
        AddressOfExpressionNode addressOf = node.Value.ShouldBeOfType<AddressOfExpressionNode>();
        addressOf.VariableName.ShouldBe("Number");
    }

    /// <summary>
    /// Tests that a VIEW FROM expression used as a declaration initial value produces a <see cref="DereferenceExpressionNode"/> with the pointer name set correctly.
    /// </summary>
    [Fact]
    public void Parse_DereferenceExpression_ValidSyntax_ParsesCorrectly()
    {
        DeclarationNode node = this.ParseFirstStatement<DeclarationNode>(
            "PRAY WELCOME Number2 AS A PEER BEING VIEW FROM NumberPointer");

        DereferenceExpressionNode dereference = node.InitialValue.ShouldBeOfType<DereferenceExpressionNode>();
        dereference.PointerName.ShouldBe("NumberPointer");
    }

    /// <summary>
    /// Tests that a VIEW FROM … IS APPOINTED statement produces a <see cref="DereferenceAssignmentNode"/> with the pointer name and new value set correctly.
    /// </summary>
    [Fact]
    public void Parse_DereferenceAssignment_ValidSyntax_ParsesCorrectly()
    {
        DereferenceAssignmentNode node = this.ParseFirstStatement<DereferenceAssignmentNode>(
            "VIEW FROM NumberPointer IS APPOINTED 23");

        node.PointerName.ShouldBe("NumberPointer");
        LiteralNode value = node.Value.ShouldBeOfType<LiteralNode>();
        value.Value.ShouldBe(23);
    }

    /// <summary>
    /// Tests that pointer arithmetic reassigned onto the same pointer variable produces an <see cref="AssignmentNode"/> whose value is a <see cref="PrefixExpressionNode"/> using the <see cref="Operator.Sum"/> operator.
    /// </summary>
    [Fact]
    public void Parse_PointerArithmetic_ValidSyntax_ParsesCorrectly()
    {
        AssignmentNode node = this.ParseFirstStatement<AssignmentNode>(
            "ValuesPointer IS APPOINTED SUM OF ValuesPointer AND 2");

        node.Target.ShouldBe("ValuesPointer");
        PrefixExpressionNode prefix = node.Value.ShouldBeOfType<PrefixExpressionNode>();
        prefix.Operator.ShouldBe(Operator.Sum);
        prefix.Arguments.Count.ShouldBe(2);
        IdentifierNode firstArgument = prefix.Arguments[0].ShouldBeOfType<IdentifierNode>();
        firstArgument.Name.ShouldBe("ValuesPointer");
    }

    /// <summary>
    /// Tests that a PRINCIPALS block containing a pointer declaration alongside a scalar declaration produces a <see cref="PrincipalBlockNode"/> with declarations in source order.
    /// </summary>
    [Fact]
    public void Parse_PrincipalsBlock_WithPointerDeclaration_PreservesSourceOrder()
    {
        string source = """
            PRINCIPALS
              PRAY WELCOME Number AS A PEER BEING 42
              PRAY WELCOME NumberPointer AS A GALLERY PICTURE OF PEER
            THE CURTAIN RISES.
            """;

        PrincipalBlockNode node = this.ParseFirstStatement<PrincipalBlockNode>(source);

        node.Declarations.Count.ShouldBe(2);
        node.Declarations[0].ShouldBeOfType<DeclarationNode>();
        node.Declarations[1].ShouldBeOfType<PointerDeclarationNode>();
    }

    /// <summary>
    /// Parses a single statement of a specific type from a source string.
    /// </summary>
    /// <typeparam name="T">The type of statement to parse.</typeparam>
    /// <param name="statementSource">The source string containing the statement.</param>
    private T ParseFirstStatement<T>(string statementSource) where T : Statement
    {
        ProgramNode program = this.parser.Parse($"HARK! \"T\" {statementSource} FINALE.");
        Statement statement = program.Statements.ShouldHaveSingleItem();
        return statement.ShouldBeOfType<T>();
    }
}
