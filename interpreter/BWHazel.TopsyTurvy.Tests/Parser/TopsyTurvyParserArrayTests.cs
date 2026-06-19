using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Parser;

namespace BWHazel.TopsyTurvy.Tests.Parser;

/// <summary>
/// Tests for array statement and expression forms parsed by the <see cref="TopsyTurvyParser"/> class.
/// </summary>
public class TopsyTurvyParserArrayTests
{
    private readonly TopsyTurvyParser parser = new();

    /// <summary>
    /// Tests that an array declaration with initial values produces an <see cref="ArrayDeclarationNode"/> with the name, element type, and initialiser list set correctly.
    /// </summary>
    [Fact]
    public void Parse_ArrayDeclaration_WithInitialValues_ParsesCorrectly()
    {
        ArrayDeclarationNode node = this.ParseFirstStatement<ArrayDeclarationNode>(
            """PRAY WELCOME miscreants AS A LITTLE LIST OF YARN BEING "Miscreant1" AND "Miscreant2" IF YOU PLEASE.""");

        node.Name.ShouldBe("miscreants");
        node.ElementType.ShouldBe(LiteralType.String);
        node.IsConstant.ShouldBeFalse();
        node.InitialValues.Count.ShouldBe(2);
    }

    /// <summary>
    /// Tests that an array declaration without a BEING clause produces an <see cref="ArrayDeclarationNode"/> with an empty initialiser list.
    /// </summary>
    [Fact]
    public void Parse_ArrayDeclaration_WithoutBeingClause_ProducesEmptyInitialiserList()
    {
        ArrayDeclarationNode node = this.ParseFirstStatement<ArrayDeclarationNode>(
            "PRAY WELCOME scores AS A LITTLE LIST OF PEER");

        node.Name.ShouldBe("scores");
        node.ElementType.ShouldBe(LiteralType.Integer);
        node.InitialValues.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that an array declaration with the CONSERVATIVE modifier sets <see cref="ArrayDeclarationNode.IsConstant"/> to <c>true</c>.
    /// </summary>
    [Fact]
    public void Parse_ArrayDeclaration_WithConservativeModifier_SetsIsConstantTrue()
    {
        ArrayDeclarationNode node = this.ParseFirstStatement<ArrayDeclarationNode>(
            "PRAY WELCOME pi AS A CONSERVATIVE LITTLE LIST OF FATHOM BEING 3.14 IF YOU PLEASE.");

        node.IsConstant.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that each scalar type keyword maps to the correct <see cref="LiteralType"/> on the <see cref="ArrayDeclarationNode.ElementType"/> property.
    /// </summary>
    /// <param name="keyword">The type keyword to test.</param>
    /// <param name="expectedType">The expected element <see cref="LiteralType"/>.</param>
    [Theory]
    [InlineData("PEER",   LiteralType.Integer)]
    [InlineData("FATHOM", LiteralType.Float)]
    [InlineData("YARN",   LiteralType.String)]
    [InlineData("DECREE", LiteralType.Boolean)]
    [InlineData("NAUGHT", LiteralType.Null)]
    public void Parse_ArrayDeclaration_WithTypeKeyword_MapsToCorrectElementType(string keyword, LiteralType expectedType)
    {
        ArrayDeclarationNode node = this.ParseFirstStatement<ArrayDeclarationNode>(
            $"PRAY WELCOME arr AS A LITTLE LIST OF {keyword}");

        node.ElementType.ShouldBe(expectedType);
    }

    /// <summary>
    /// Tests that a VICTIM...ON expression produces an <see cref="ArrayIndexNode"/> with the index and array name set correctly.
    /// </summary>
    [Fact]
    public void Parse_ArrayIndexExpression_ValidSyntax_ParsesCorrectly()
    {
        ArrayIndexNode node = this.ParseFirstExpressionStatement<ArrayIndexNode>(
            "VICTIM 1 ON miscreants");

        node.ArrayName.ShouldBe("miscreants");
        LiteralNode index = node.Index.ShouldBeOfType<LiteralNode>();
        index.Value.ShouldBe(1);
    }

    /// <summary>
    /// Tests that a VICTIM…ON IS APPOINTED statement produces an <see cref="ArrayElementAssignmentNode"/> with the index, array name and new value set correctly.
    /// </summary>
    [Fact]
    public void Parse_ArrayElementAssignment_ValidSyntax_ParsesCorrectly()
    {
        ArrayElementAssignmentNode node = this.ParseFirstStatement<ArrayElementAssignmentNode>(
            """VICTIM 2 ON miscreants IS APPOINTED "OtherMiscreant" """);

        node.ArrayName.ShouldBe("miscreants");
        LiteralNode index = node.Index.ShouldBeOfType<LiteralNode>();
        index.Value.ShouldBe(2);
        LiteralNode value = node.Value.ShouldBeOfType<LiteralNode>();
        value.Value.ShouldBe("OtherMiscreant");
    }

    /// <summary>
    /// Tests that an array declaration with a size literal sets <see cref="ArrayDeclarationNode.Size"/> to that value.
    /// </summary>
    [Fact]
    public void Parse_ArrayDeclaration_WithSize_SetsSize()
    {
        ArrayDeclarationNode node = this.ParseFirstStatement<ArrayDeclarationNode>(
            "PRAY WELCOME arr AS A LITTLE LIST OF 3 YARN");

        node.Size.ShouldBe(3);
        node.InitialValues.ShouldBeEmpty();
    }

    /// <summary>
    /// Tests that an array declaration without a size literal leaves <see cref="ArrayDeclarationNode.Size"/> as <c>null</c>.
    /// </summary>
    [Fact]
    public void Parse_ArrayDeclaration_WithoutSize_LeavesSize_Null()
    {
        ArrayDeclarationNode node = this.ParseFirstStatement<ArrayDeclarationNode>(
            "PRAY WELCOME arr AS A LITTLE LIST OF YARN");

        node.Size.ShouldBeNull();
    }

    /// <summary>
    /// Tests that a zero-sized array declaration is parsed as <see cref="ArrayDeclarationNode.Size"/> equal to zero.
    /// </summary>
    [Fact]
    public void Parse_ArrayDeclaration_WithZeroSize_SetsSizeToZero()
    {
        ArrayDeclarationNode node = this.ParseFirstStatement<ArrayDeclarationNode>(
            "PRAY WELCOME arr AS A LITTLE LIST OF 0 YARN");

        node.Size.ShouldBe(0);
    }

    /// <summary>
    /// Tests that a PRINCIPALS block containing mixed declarations produces a <see cref="PrincipalBlockNode"/> with declarations in source order.
    /// </summary>
    [Fact]
    public void Parse_PrincipalsBlock_WithMixedDeclarations_PreservesSourceOrder()
    {
        string source = """
            PRINCIPALS
              PRAY WELCOME alpha AS A PEER BEING 1
              PRAY WELCOME names AS A LITTLE LIST OF YARN BEING "a" AND "b" IF YOU PLEASE.
            THE CURTAIN RISES.
            """;

        PrincipalBlockNode node = this.ParseFirstStatement<PrincipalBlockNode>(source);

        node.Declarations.Count.ShouldBe(2);
        node.Declarations[0].ShouldBeOfType<DeclarationNode>();
        node.Declarations[1].ShouldBeOfType<ArrayDeclarationNode>();
    }

    /// <summary>
    /// Tests that a <c>RECKONING OF</c> expression on an identifier parses into an <see cref="ArrayLengthNode"/> with the array name set correctly.
    /// </summary>
    [Fact]
    public void Parse_ArrayLengthExpression_ValidSyntax_ParsesCorrectly()
    {
        ArrayLengthNode node = this.ParseFirstExpressionStatement<ArrayLengthNode>(
            "RECKONING OF miscreants");

        node.ArrayName.ShouldBe("miscreants");
    }

    /// <summary>
    /// Tests that a <c>RECKONING OF THE PROPS</c> expression parses into an <see cref="ArrayLengthNode"/> with the array name set to the <c>THE PROPS</c> built-in.
    /// </summary>
    [Fact]
    public void Parse_ArrayLengthExpression_TheProps_ParsesCorrectly()
    {
        ArrayLengthNode node = this.ParseFirstExpressionStatement<ArrayLengthNode>(
            "RECKONING OF THE PROPS");

        node.ArrayName.ShouldBe("THE PROPS");
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

    /// <summary>
    /// Parses a single standalone expression statement and returns the inner expression cast as <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The expected <see cref="Expression"/> type wrapped by the <see cref="ExpressionStatement"/>.</typeparam>
    /// <param name="statementSource">The source text for the expression statement.</param>
    private T ParseFirstExpressionStatement<T>(string statementSource) where T : Expression
    {
        ProgramNode program = this.parser.Parse($"HARK! \"T\" {statementSource} FINALE.");
        Statement statement = program.Statements.ShouldHaveSingleItem();
        ExpressionStatement expressionStatement = statement.ShouldBeOfType<ExpressionStatement>();
        return expressionStatement.Expression.ShouldBeOfType<T>();
    }
}
