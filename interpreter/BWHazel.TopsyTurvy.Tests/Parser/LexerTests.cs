using Superpower.Model;
using BWHazel.TopsyTurvy.Parser;

namespace BWHazel.TopsyTurvy.Tests.Parser;

/// <summary>
/// Tests for the atomic <see cref="Lexer"/> parser combinators.
/// </summary>
/// <remarks>
/// <c>Lexer.Whitespace</c>, <c>Lexer.WhitespaceRequired</c>, and <c>Lexer.IntegerLiteral</c>
/// are not tested here.  Each is a single-line delegation to the Superpower library primitive with
/// no custom logic (Character.WhiteSpace.Many(), Character.WhiteSpace.AtLeastOnce(), and
/// Numerics.IntegerInt32 respectively) therefore testing them would verify that the Superpower library
/// works, not Topsy Turvy lexer code.
/// </remarks>
public class LexerTests
{
    /// <summary>
    /// Tests that the <see cref="Lexer.StringLiteral"/> parser produces an empty string for an empty double-quoted literal.
    /// </summary>
    [Fact]
    public void StringLiteral_WithEmptyString_ReturnsEmptyString()
    {
        var result = Lexer.StringLiteral(new("\"\""));

        Assert.True(result.HasValue);
        Assert.Equal(string.Empty, result.Value);
    }

    /// <summary>
    /// Tests that the <see cref="Lexer.StringLiteral"/> parser returns string content without the surrounding quotes.
    /// </summary>
    [Fact]
    public void StringLiteral_WithSimpleContent_ReturnsContent()
    {
        var result = Lexer.StringLiteral(new("\"hello\""));

        Assert.True(result.HasValue);
        Assert.Equal("hello", result.Value);
    }

    /// <summary>
    /// Tests that the <see cref="Lexer.StringLiteral"/> parser expands the tilde-n escape sequence to a new-line character.
    /// </summary>
    [Fact]
    public void StringLiteral_WithNewlineEscape_ExpandsToNewline()
    {
        var result = Lexer.StringLiteral(new("\"line1~nline2\""));

        Assert.True(result.HasValue);
        Assert.Equal("line1\nline2", result.Value);
    }

    /// <summary>
    /// Tests that the <see cref="Lexer.StringLiteral"/> parser expands the tilde-t escape sequence to a tab character.
    /// </summary>
    [Fact]
    public void StringLiteral_WithTabEscape_ExpandsToTab()
    {
        var result = Lexer.StringLiteral(new("\"col1~tcol2\""));

        Assert.True(result.HasValue);
        Assert.Equal("col1\tcol2", result.Value);
    }

    /// <summary>
    /// Tests that the <see cref="Lexer.StringLiteral"/> parser expands the tilde-double-quote escape sequence to a double-quote character.
    /// </summary>
    [Fact]
    public void StringLiteral_WithDoubleQuoteEscape_ExpandsToDoubleQuote()
    {
        var result = Lexer.StringLiteral(new("\"say ~\"hello~\"\""));

        Assert.True(result.HasValue);
        Assert.Equal("say \"hello\"", result.Value);
    }

    /// <summary>
    /// Tests that the <see cref="Lexer.StringLiteral"/> parser expands the tilde-tilde escape sequence to a single tilde character.
    /// </summary>
    [Fact]
    public void StringLiteral_WithTildeEscape_ExpandsToTilde()
    {
        var result = Lexer.StringLiteral(new("\"a~~b\""));

        Assert.True(result.HasValue);
        Assert.Equal("a~b", result.Value);
    }

    /// <summary>
    /// Tests that the <see cref="Lexer.StringLiteral"/> parser fails when the string literal has no closing quote.
    /// </summary>
    [Fact]
    public void StringLiteral_WithoutClosingQuote_Fails()
    {
        var result = Lexer.StringLiteral(new("\"unclosed"));

        Assert.False(result.HasValue);
    }

    /// <summary>
    /// Tests that the <see cref="Lexer.FloatLiteral"/> parser produces the correct double value for a valid floating-point literal.
    /// </summary>
    [Fact]
    public void FloatLiteral_WithValidFloat_ReturnsDouble()
    {
        var result = Lexer.FloatLiteral(new("3.14"));

        Assert.True(result.HasValue);
        Assert.Equal(3.14, result.Value, precision: 10);
    }

    /// <summary>
    /// Tests that the <see cref="Lexer.FloatLiteral"/> parser fails and backtracks when given an integer with no decimal point.
    /// </summary>
    [Fact]
    public void FloatLiteral_WithIntegerWithoutDecimalPoint_Fails()
    {
        var result = Lexer.FloatLiteral(new("42"));

        Assert.False(result.HasValue);
    }

    /// <summary>
    /// Tests that the <see cref="Lexer.FloatLiteral"/> parser fails when there are no fractional digits following the decimal point.
    /// </summary>
    [Fact]
    public void FloatLiteral_WithNoFractionalDigits_Fails()
    {
        var result = Lexer.FloatLiteral(new("1."));

        Assert.False(result.HasValue);
    }

    /// <summary>
    /// Tests that the <see cref="Lexer.BooleanLiteral"/> parser produces <c>true</c> for the VERITY keyword.
    /// </summary>
    [Theory]
    [InlineData("VERITY")]
    [InlineData("verity")]
    public void BooleanLiteral_WithVerity_ReturnsTrue(string input)
    {
        var result = Lexer.BooleanLiteral(new(input));

        Assert.True(result.HasValue);
        Assert.True(result.Value);
    }

    /// <summary>
    /// Tests that the <see cref="Lexer.BooleanLiteral"/> parser produces <c>false</c> for the NAY keyword.
    /// </summary>
    [Theory]
    [InlineData("NAY")]
    [InlineData("nay")]
    public void BooleanLiteral_WithNay_ReturnsFalse(string input)
    {
        var result = Lexer.BooleanLiteral(new(input));

        Assert.True(result.HasValue);
        Assert.False(result.Value);
    }

    /// <summary>
    /// Tests that the <see cref="Lexer.BooleanLiteral"/> parser fails for a word that is neither VERITY nor NAY.
    /// </summary>
    [Fact]
    public void BooleanLiteral_WithUnrecognisedWord_Fails()
    {
        var result = Lexer.BooleanLiteral(new("TRUE"));

        Assert.False(result.HasValue);
    }

    /// <summary>
    /// Tests that the <see cref="Lexer.NullLiteral"/> parser produces a null value for theNAUGHT keyword.
    /// </summary>
    [Theory]
    [InlineData("NAUGHT")]
    [InlineData("naught")]
    public void NullLiteral_WithNaught_ReturnsNull(string input)
    {
        var result = Lexer.NullLiteral(new(input));

        Assert.True(result.HasValue);
        Assert.Null(result.Value);
    }

    /// <summary>
    /// Tests that the <see cref="Lexer.NullLiteral"/> parser fails for a word other than NAUGHT.
    /// </summary>
    [Fact]
    public void NullLiteral_WithUnrecognisedWord_Fails()
    {
        var result = Lexer.NullLiteral(new("NULL"));

        Assert.False(result.HasValue);
    }

    /// <summary>
    /// Tests that the <see cref="Lexer.Identifier"/> parser accepts a simple alphabetic name as a valid identifier.
    /// </summary>
    [Fact]
    public void Identifier_WithSimpleAlphabeticName_ReturnsName()
    {
        var result = Lexer.Identifier(new("greeting"));

        Assert.True(result.HasValue);
        Assert.Equal("greeting", result.Value);
    }

    /// <summary>
    /// Tests that the <see cref="Lexer.Identifier"/> parser accepts an identifier containing a hyphen and a digit.
    /// </summary>
    [Fact]
    public void Identifier_WithHyphenAndDigit_ReturnsName()
    {
        var result = Lexer.Identifier(new("Ko-Ko2"));

        Assert.True(result.HasValue);
        Assert.Equal("Ko-Ko2", result.Value);
    }

    /// <summary>
    /// Tests that the <see cref="Lexer.Identifier"/> parser accepts an identifier containing an underscore.
    /// </summary>
    [Fact]
    public void Identifier_WithUnderscore_ReturnsName()
    {
        var result = Lexer.Identifier(new("my_var"));

        Assert.True(result.HasValue);
        Assert.Equal("my_var", result.Value);
    }

    /// <summary>
    /// Tests that the <see cref="Lexer.Identifier"/> parser rejects each word in its exclusion list.
    /// </summary>
    /// <param name="keyword">The excluded keyword to test.</param>
    [Theory]
    [InlineData("HARK")]
    [InlineData("FINALE")]
    [InlineData("PRINCIPALS")]
    [InlineData("BEHOLD")]
    [InlineData("NAUGHT")]
    [InlineData("VERITY")]
    [InlineData("NAY")]
    [InlineData("SO")]
    [InlineData("QUITE")]
    [InlineData("WHEN")]
    [InlineData("NOTHING")]
    [InlineData("hark")]
    [InlineData("finale")]
    [InlineData("principals")]
    [InlineData("behold")]
    [InlineData("naught")]
    [InlineData("verity")]
    [InlineData("nay")]
    [InlineData("so")]
    [InlineData("quite")]
    [InlineData("when")]
    [InlineData("nothing")]
    public void Identifier_WithExcludedKeyword_Fails(string keyword)
    {
        var result = Lexer.Identifier(new(keyword));

        Assert.False(result.HasValue);
    }

    /// <summary>
    /// Tests that the <see cref="Lexer.Keyword"/> method matches the exact keyword text and returns it in upper case.
    /// </summary>
    [Fact]
    public void Keyword_WithExactMatchUpperCase_ReturnsUpperCase()
    {
        var result = Lexer.Keyword("BEHOLD")(new("BEHOLD"));

        Assert.True(result.HasValue);
        Assert.Equal("BEHOLD", result.Value);
    }

    /// <summary>
    /// Tests that the <see cref="Lexer.Keyword"/> method matches case-insensitively and returns the upper-case form.
    /// </summary>
    [Fact]
    public void Keyword_WithLowerCaseInput_ReturnsUpperCase()
    {
        var result = Lexer.Keyword("BEHOLD")(new("behold"));

        Assert.True(result.HasValue);
        Assert.Equal("BEHOLD", result.Value);
    }

    /// <summary>
    /// Tests that the <see cref="Lexer.Keyword"/> method matches mixed-case input and returns the upper-case form.
    /// </summary>
    [Fact]
    public void Keyword_WithMixedCaseInput_ReturnsUpperCase()
    {
        var result = Lexer.Keyword("BEHOLD")(new("BeHoLd"));

        Assert.True(result.HasValue);
        Assert.Equal("BEHOLD", result.Value);
    }

    /// <summary>
    /// Tests that the <see cref="Lexer.Keyword"/> method fails when the input does not match the expected keyword.
    /// </summary>
    [Fact]
    public void Keyword_WithMismatchedInput_Fails()
    {
        var result = Lexer.Keyword("BEHOLD")(new("FINALE"));

        Assert.False(result.HasValue);
    }
}
