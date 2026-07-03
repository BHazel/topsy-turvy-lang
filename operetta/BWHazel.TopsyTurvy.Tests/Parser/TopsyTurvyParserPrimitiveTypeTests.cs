using System.Linq;
using BWHazel.TopsyTurvy.Ast;
using BWHazel.TopsyTurvy.Parser;

namespace BWHazel.TopsyTurvy.Tests.Parser;

/// <summary>
/// Tests for parsing primitive types, type keyword declarations an in-place casts for all supported types.
/// </summary>
public class TopsyTurvyParserPrimitiveTypeTests
{
    private readonly TopsyTurvyParser parser = new();

    /// <summary>
    /// Tests that a single-quoted character literal produces a <see cref="LiteralNode"/> with type
    /// <see cref="LiteralType.Char"/> and the correct character value, including all supported escape sequences.
    /// </summary>
    /// <param name="source">The character literal source text.</param>
    /// <param name="expectedValue">The expected resolved <see cref="char"/> value.</param>
    [Theory]
    [InlineData("'A'",   'A')]
    [InlineData("'~n'",  '\n')]
    [InlineData("'~t'",  '\t')]
    [InlineData("'~''",  '\'')]
    [InlineData("'~~'",  '~')]
    public void Parse_WithCharLiteral_ProducesCorrectCharValue(string source, char expectedValue)
    {
        LiteralNode node = this.ParsePrintExpression<LiteralNode>(source);

        node.Type.ShouldBe(LiteralType.Char);
        node.Value.ShouldBe(expectedValue);
    }

    /// <summary>
    /// Tests that each type keyword in a <c>PRAY WELCOME</c> declaration maps to the correct
    /// <see cref="LiteralType"/> on the <see cref="DeclarationNode"/>.
    /// </summary>
    /// <param name="keyword">The Topsy Turvy type keyword or keyword phrase.</param>
    /// <param name="expectedType">The expected <see cref="LiteralType"/> corresponding to the keyword.</param>
    [Theory]
    [InlineData("PEER",                   LiteralType.Integer)]
    [InlineData("CHANCELLOR",             LiteralType.Long)]
    [InlineData("PIRATE",                 LiteralType.Short)]
    [InlineData("SAUSAGE-ROLL",           LiteralType.SignedByte)]
    [InlineData("STANDING PEER",          LiteralType.UnsignedInteger)]
    [InlineData("STANDING CHANCELLOR",    LiteralType.UnsignedLong)]
    [InlineData("STANDING PIRATE",        LiteralType.UnsignedShort)]
    [InlineData("STANDING SAUSAGE-ROLL",  LiteralType.Byte)]
    [InlineData("FATHOM",                 LiteralType.Double)]
    [InlineData("FOOT",                   LiteralType.Single)]
    [InlineData("YARN",                   LiteralType.String)]
    [InlineData("STITCH",                 LiteralType.Char)]
    [InlineData("DECREE",                 LiteralType.Boolean)]
    [InlineData("NAUGHT",                 LiteralType.Null)]
    public void Parse_WithTypeAnnotation_MapsToCorrectLiteralType(string keyword, LiteralType expectedType)
    {
        DeclarationNode node = this.ParseDeclaration($"PRAY WELCOME x AS A {keyword}");

        node.Type.ShouldBe(expectedType);
    }

    /// <summary>
    /// Parses a print expression from the given source test.
    /// </summary>
    /// <typeparam name="T">The expected type of the expression.</typeparam>
    /// <param name="expressionSource">The source code of the expression.</param>
    /// <returns>The parsed expression of type <typeparamref name="T"/>.</returns>
    private T ParsePrintExpression<T>(string expressionSource) where T : Expression
    {
        ProgramNode program = this.parser.Parse($"HARK! \"T\" BEHOLD {expressionSource} FINALE.");
        PrintNode print = program.Statements.ShouldHaveSingleItem().ShouldBeOfType<PrintNode>();
        return print.Expression.ShouldBeOfType<T>();
    }

    /// <summary>
    /// Parses a declaration from the given source text.
    /// </summary>
    /// <param name="declarationSource">The source code of the declaration.</param>
    /// <returns>The parsed declaration node.</returns>
    private DeclarationNode ParseDeclaration(string declarationSource)
    {
        ProgramNode program = this.parser.Parse($"HARK! \"T\" {declarationSource} FINALE.");
        return program.Statements.ShouldHaveSingleItem().ShouldBeOfType<DeclarationNode>();
    }
}
