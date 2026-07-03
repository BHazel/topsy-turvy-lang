using BWHazel.TopsyTurvy.UtopIR.Parser;

namespace BWHazel.TopsyTurvy.UtopIR.Tests.Parser;

/// <summary>
/// Tests for the atomic <see cref="Lexer"/> parser combinators.
/// </summary>
/// <remarks>
/// <c>Lexer.Whitespace</c>, <c>Lexer.WhitespaceRequired</c>, and <c>Lexer.IntegerLiteral</c> are not
/// tested directly here: each is a thin delegation to a Superpower primitive with no custom logic.
/// </remarks>
public class LexerTests
{
    /// <summary>
    /// Tests that <see cref="Lexer.Variable"/> strips the <c>£</c> character and returns the identifier text.
    /// </summary>
    [Fact]
    public void Variable_WithSimpleName_ReturnsNameWithoutSigil()
    {
        var result = Lexer.Variable(new("£LovesickMaidens"));

        result.HasValue.ShouldBeTrue();
        result.Value.ShouldBe("LovesickMaidens");
    }

    /// <summary>
    /// Tests that <see cref="Lexer.Variable"/> accepts a temporary register name beginning with an underscore.
    /// </summary>
    [Fact]
    public void Variable_WithUnderscorePrefixedTempName_ReturnsName()
    {
        var result = Lexer.Variable(new("£_sum_Peer1_Peer2"));

        result.HasValue.ShouldBeTrue();
        result.Value.ShouldBe("_sum_Peer1_Peer2");
    }

    /// <summary>
    /// Tests that <see cref="Lexer.Variable"/> fails without the leading <c>£</c> character.
    /// </summary>
    [Fact]
    public void Variable_WithoutSigil_Fails()
    {
        var result = Lexer.Variable(new("LovesickMaidens"));

        result.HasValue.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that <see cref="Lexer.StringLiteral"/> returns string content without surrounding quotes.
    /// </summary>
    [Fact]
    public void StringLiteral_WithSimpleContent_ReturnsContent()
    {
        var result = Lexer.StringLiteral(new("\"hello\""));

        result.HasValue.ShouldBeTrue();
        result.Value.ShouldBe("hello");
    }

    /// <summary>
    /// Tests that <see cref="Lexer.StringLiteral"/> resolves <c>~</c>-escape sequences.
    /// </summary>
    [Fact]
    public void StringLiteral_WithEscapes_ResolvesEscapeSequences()
    {
        var result = Lexer.StringLiteral(new("\"a~\"b~~c~nd\""));

        result.HasValue.ShouldBeTrue();
        result.Value.ShouldBe("a\"b~c\nd");
    }

    /// <summary>
    /// Tests that <see cref="Lexer.CharacterLiteral"/> returns the single resolved character.
    /// </summary>
    [Fact]
    public void CharacterLiteral_WithSimpleContent_ReturnsCharacter()
    {
        var result = Lexer.CharacterLiteral(new("'A'"));

        result.HasValue.ShouldBeTrue();
        result.Value.ShouldBe('A');
    }

    /// <summary>
    /// Tests that <see cref="Lexer.CharacterLiteral"/> resolves an escape sequence.
    /// </summary>
    [Fact]
    public void CharacterLiteral_WithEscape_ResolvesEscapeSequence()
    {
        var result = Lexer.CharacterLiteral(new("'~n'"));

        result.HasValue.ShouldBeTrue();
        result.Value.ShouldBe('\n');
    }

    /// <summary>
    /// Tests that <see cref="Lexer.FloatLiteral"/> parses a whole-number float with its decimal point.
    /// </summary>
    [Fact]
    public void FloatLiteral_WithWholeNumber_ReturnsDouble()
    {
        var result = Lexer.FloatLiteral(new("5.0"));

        result.HasValue.ShouldBeTrue();
        result.Value.ShouldBe(5.0);
    }

    /// <summary>
    /// Tests that <see cref="Lexer.FloatLiteral"/> parses a fractional value.
    /// </summary>
    [Fact]
    public void FloatLiteral_WithFraction_ReturnsDouble()
    {
        var result = Lexer.FloatLiteral(new("3.14"));

        result.HasValue.ShouldBeTrue();
        result.Value.ShouldBe(3.14);
    }

    /// <summary>
    /// Tests that <see cref="Lexer.BooleanLiteral"/> parses <c>verity</c> as <c>true</c>.
    /// </summary>
    [Fact]
    public void BooleanLiteral_WithVerity_ReturnsTrue()
    {
        var result = Lexer.BooleanLiteral(new("verity"));

        result.HasValue.ShouldBeTrue();
        result.Value.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that <see cref="Lexer.BooleanLiteral"/> parses <c>nay</c> as <c>false</c>.
    /// </summary>
    [Fact]
    public void BooleanLiteral_WithNay_ReturnsFalse()
    {
        var result = Lexer.BooleanLiteral(new("nay"));

        result.HasValue.ShouldBeTrue();
        result.Value.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that <see cref="Lexer.Comment"/> matches from <c>@</c> to the end of the text, excluding the newline.
    /// </summary>
    [Fact]
    public void Comment_WithTrailingNewline_StopsBeforeNewline()
    {
        var result = Lexer.Comment(new("@ a comment\nnext line"));

        result.HasValue.ShouldBeTrue();
        result.Value.ShouldBe(" a comment");
    }

    /// <summary>
    /// Tests that <see cref="Lexer.Identifier"/> accepts a leading letter.
    /// </summary>
    [Fact]
    public void Identifier_WithLeadingLetter_ReturnsName()
    {
        var result = Lexer.Identifier(new("Peer1"));

        result.HasValue.ShouldBeTrue();
        result.Value.ShouldBe("Peer1");
    }

    /// <summary>
    /// Tests that <see cref="Lexer.Identifier"/> accepts a leading underscore, matching temporary register names.
    /// </summary>
    [Fact]
    public void Identifier_WithLeadingUnderscore_ReturnsName()
    {
        var result = Lexer.Identifier(new("_sum_a_b"));

        result.HasValue.ShouldBeTrue();
        result.Value.ShouldBe("_sum_a_b");
    }

    /// <summary>
    /// Tests that <see cref="Lexer.BoxIntegerLiteralValue(long)"/> boxes an in-range value as <see cref="int"/>.
    /// </summary>
    [Fact]
    public void BoxIntegerLiteralValue_WithInRangeValue_BoxesAsInt()
    {
        object boxed = Lexer.BoxIntegerLiteralValue(200);

        boxed.ShouldBeOfType<int>();
        boxed.ShouldBe(200);
    }

    /// <summary>
    /// Tests that <see cref="Lexer.BoxIntegerLiteralValue(long)"/> boxes an out-of-range value as <see cref="long"/>.
    /// </summary>
    [Fact]
    public void BoxIntegerLiteralValue_WithOutOfRangeValue_BoxesAsLong()
    {
        object boxed = Lexer.BoxIntegerLiteralValue(5_000_000_000L);

        boxed.ShouldBeOfType<long>();
        boxed.ShouldBe(5_000_000_000L);
    }

    /// <summary>
    /// Tests that <see cref="Lexer.Keyword(string)"/> matches case-insensitively and returns the canonical lowercase form.
    /// </summary>
    [Fact]
    public void Keyword_WithMixedCaseInput_ReturnsLowercaseCanonicalForm()
    {
        var result = Lexer.Keyword("welcome")(new("WELCOME"));

        result.HasValue.ShouldBeTrue();
        result.Value.ShouldBe("welcome");
    }
}
