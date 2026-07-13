using System.Linq;
using BWHazel.TopsyTurvy.Analysis;

namespace BWHazel.TopsyTurvy.Tests.Analysis;

/// <summary>
/// Tests for <see cref="SourceTokeniser"/>.
/// </summary>
public class SourceTokeniserTests
{
    /// <summary>
    /// Tests that the <see cref="SourceTokeniser.Tokenise"/> method categorises a single-line <c>ASIDE:</c> comment as a single "comment" token confined to that line.
    /// </summary>
    [Fact]
    public void Tokenise_WithLineComment_ReturnsSingleLineCommentToken()
    {
        SourceToken token = SourceTokeniser.Tokenise("ASIDE: a remark\nBEHOLD \"after\"")
            .Single(token => token.Category == "comment");

        token.Span.Start.Line.ShouldBe(1);
        token.Span.End.Line.ShouldBe(1);
    }

    /// <summary>
    /// Tests that the <see cref="SourceTokeniser.Tokenise"/> method categorises a terminated block comment as a single "comment" token spanning from its opener to its closer, even across multiple lines.
    /// </summary>
    [Fact]
    public void Tokenise_WithTerminatedBlockComment_ReturnsSingleTokenSpanningToCloser()
    {
        SourceToken token = SourceTokeniser.Tokenise("(ASIDE, AT SOME LENGTH:\nspans\nlines\nEND OF ASIDE.)\nBEHOLD \"after\"")
            .Single(token => token.Category == "comment");

        token.Span.Start.Line.ShouldBe(1);
        token.Span.End.Line.ShouldBe(4);
    }

    /// <summary>
    /// Tests that the <see cref="SourceTokeniser.Tokenise"/> method categorises an unterminated block comment as a single "comment" token extending to end-of-source, rather than throwing.
    /// </summary>
    [Fact]
    public void Tokenise_WithUnterminatedBlockComment_ReturnsTokenExtendingToEndOfSource()
    {
        SourceToken token = SourceTokeniser.Tokenise("(ASIDE, AT SOME LENGTH:\nnever closed")
            .Single(token => token.Category == "comment");

        token.Span.Start.Line.ShouldBe(1);
        token.Span.End.Line.ShouldBe(2);
    }

    /// <summary>
    /// Tests that the <see cref="SourceTokeniser.Tokenise"/> method categorises a double-quoted string literal, including one containing a <c>~</c>-escaped quote, as a single "string" token.
    /// </summary>
    [Fact]
    public void Tokenise_WithStringLiteralContainingEscapedQuote_ReturnsSingleStringToken()
    {
        SourceToken token = SourceTokeniser.Tokenise("BEHOLD \"a ~\"quoted~\" word\"")
            .Single(token => token.Category == "string");

        token.Span.Start.Column.ShouldBe(8);
    }

    /// <summary>
    /// Tests that the <see cref="SourceTokeniser.Tokenise"/> method categorises a single-quoted character literal as a "string" token.
    /// </summary>
    [Fact]
    public void Tokenise_WithCharacterLiteral_ReturnsStringToken()
    {
        SourceTokeniser.Tokenise("PRAY WELCOME letter AS A STITCH BEING 'x'")
            .ShouldContain(token => token.Category == "string");
    }

    /// <summary>
    /// Tests that the <see cref="SourceTokeniser.Tokenise"/> method categorises an integer literal and a floating-point literal, including a negative value, as "number" tokens.
    /// </summary>
    [Fact]
    public void Tokenise_WithIntegerAndFloatLiterals_ReturnsNumberTokens()
    {
        SourceToken[] numbers = [.. SourceTokeniser.Tokenise("BEING -42 BEING 3.14")
            .Where(token => token.Category == "number")];

        numbers.Length.ShouldBe(2);
    }

    /// <summary>
    /// Tests that the <see cref="SourceTokeniser.Tokenise"/> method categorises a plain language keyword as a "keyword" token.
    /// </summary>
    [Fact]
    public void Tokenise_WithPlainKeyword_ReturnsKeywordToken()
    {
        SourceTokeniser.Tokenise("BEHOLD \"Test\"")
            .ShouldContain(token => token.Category == "keyword" && token.Span.Start.Column == 1);
    }

    /// <summary>
    /// Tests that the <see cref="SourceTokeniser.Tokenise"/> method categorises a multi-word keyword phrase as a single "keyword" token spanning the whole phrase.
    /// </summary>
    [Fact]
    public void Tokenise_WithMultiWordKeywordPhrase_ReturnsSingleKeywordTokenSpanningWholePhrase()
    {
        SourceToken token = SourceTokeniser.Tokenise("PRAY WELCOME x AS A PEER BEING 1")
            .Single(token => token.Category == "keyword" && token.Span.Start.Column == 1);

        token.Span.End.Column.ShouldBe("PRAY WELCOME".Length + 1);
    }

    /// <summary>
    /// Tests that the <see cref="SourceTokeniser.Tokenise"/> method resolves a keyword-phrase prefix collision by matching the longer phrase in full when it is actually present.
    /// </summary>
    [Fact]
    public void Tokenise_WithLongerKeywordPhraseContainingShorterPrefix_MatchesLongerPhraseInFull()
    {
        SourceToken token = SourceTokeniser.Tokenise("MY DUTY IS PREMATURELY DISCHARGED.")
            .Single(token => token.Category == "keyword");

        token.Span.End.Column.ShouldBe("MY DUTY IS PREMATURELY DISCHARGED.".Length + 1);
    }

    /// <summary>
    /// Tests that the <see cref="SourceTokeniser.Tokenise"/> method categorises a type-name keyword such as <c>PEER</c> as a "type" token rather than a plain "keyword" token.
    /// </summary>
    [Fact]
    public void Tokenise_WithTypeNameKeyword_ReturnsTypeToken()
    {
        SourceTokeniser.Tokenise("PRAY WELCOME x AS A PEER BEING 1")
            .ShouldContain(token => token.Category == "type");
    }

    /// <summary>
    /// Tests that the <see cref="SourceTokeniser.Tokenise"/> method categorises the <c>STANDING</c> modifier as a "keywordOther" token.
    /// </summary>
    [Fact]
    public void Tokenise_WithStandingModifier_ReturnsKeywordOtherToken()
    {
        SourceTokeniser.Tokenise("PRAY WELCOME x AS A STANDING PEER BEING 1")
            .ShouldContain(token => token.Category == "keywordOther");
    }

    /// <summary>
    /// Tests that the <see cref="SourceTokeniser.Tokenise"/> method categorises <c>THE PROPS</c> as a single "variable" token rather than two separate identifiers.
    /// </summary>
    [Fact]
    public void Tokenise_WithTheProps_ReturnsSingleVariableToken()
    {
        SourceToken token = SourceTokeniser.Tokenise("BEHOLD THE PROPS")
            .Single(token => token.Category == "variable");

        token.Span.End.Column.ShouldBe("BEHOLD THE PROPS".Length + 1);
    }

    /// <summary>
    /// Tests that the <see cref="SourceTokeniser.Tokenise"/> method categorises an ordinary user-declared name as an "identifier" token.
    /// </summary>
    [Fact]
    public void Tokenise_WithUserDeclaredName_ReturnsIdentifierToken()
    {
        SourceTokeniser.Tokenise("PRAY WELCOME myVariable AS A PEER BEING 1")
            .ShouldContain(token => token.Category == "identifier");
    }
}
