using BWHazel.TopsyTurvy.Cli.Repl;

namespace BWHazel.TopsyTurvy.Tests.Cli;

/// <summary>
/// Tests for the <see cref="ReplHighlighter"/> class.
/// </summary>
/// <remarks>
/// All tests verify markup output.  The <see cref="ReplHighlighter.Highlight"/> function is pure and produces
/// deterministic output from a given input string.
/// </remarks>
public class CadenzaHighlighterTests
{
    /// <summary>
    /// Tests that the <see cref="ReplHighlighter.Highlight"/> method returns an empty string for empty input.
    /// </summary>
    [Fact]
    public void Highlight_EmptyInput_ReturnsEmpty()
    {
        string result = ReplHighlighter.Highlight(string.Empty);

        result.ShouldBe(string.Empty);
    }

    /// <summary>
    /// Tests that the <see cref="ReplHighlighter.Highlight"/> method returns a null-or-whitespace-free plain identifier with
    /// no known keywords is returned without any markup tags.
    /// </summary>
    [Fact]
    public void Highlight_PlainIdentifier_ReturnsNoMarkup()
    {
        string result = ReplHighlighter.Highlight("myVariable");

        result.ShouldBe("myVariable");
    }

    /// <summary>
    /// Tests that the <see cref="ReplHighlighter.Highlight"/> method wraps <c>BEHOLD</c> in <c>lightgreen_1</c> markup.
    /// </summary>
    [Fact]
    public void Highlight_Behold_AppliesLightGreenMarkup()
    {
        string result = ReplHighlighter.Highlight("BEHOLD");

        result.ShouldBe("[lightgreen_1]BEHOLD[/]");
    }

    /// <summary>
    /// Tests that the <see cref="ReplHighlighter.Highlight"/> method wraps <c>HARK!</c> in <c>bold green3</c> markup (programme structure category).
    /// </summary>
    [Fact]
    public void Highlight_Hark_AppliesBoldGreen3Markup()
    {
        string result = ReplHighlighter.Highlight("HARK!");

        result.ShouldBe("[bold green3]HARK![/]");
    }

    /// <summary>
    /// Tests that the <see cref="ReplHighlighter.Highlight"/> method matches <c>SHOULD IT TRANSPIRE THAT</c> as a single compound
    /// keyword rather than <c>SHOULD</c> followed by the remainder.
    /// </summary>
    [Fact]
    public void Highlight_LongerKeywordWinsOverShorterPrefix()
    {
        string result = ReplHighlighter.Highlight("SHOULD IT TRANSPIRE THAT x");

        result.ShouldContain("[deepskyblue1]SHOULD IT TRANSPIRE THAT[/]");
        result.ShouldNotContain("[deepskyblue1]SHOULD[/]");
    }

    /// <summary>
    /// Tests that the <see cref="ReplHighlighter.Highlight"/> method wraps a string literal in <c>sandybrown</c> markup.
    /// </summary>
    [Fact]
    public void Highlight_StringLiteral_AppliesSandybrownMarkup()
    {
        string result = ReplHighlighter.Highlight("\"Hello, World!\"");

        result.ShouldContain("[sandybrown]");
        result.ShouldContain("Hello, World!");
        result.ShouldContain("[/]");
    }

    /// <summary>
    /// Tests that the <see cref="ReplHighlighter.Highlight"/> method consumes a string literal containing a tilde-escaped quote
    /// in full, not split at the escaped quote character.
    /// </summary>
    [Fact]
    public void Highlight_StringLiteralWithEscapedQuote_ConsumesFullLiteral()
    {
        string result = ReplHighlighter.Highlight("\"say ~\" hello\"");
        result.ShouldContain("[sandybrown]");
        result.ShouldContain("say ~");
    }

    /// <summary>
    /// Tests that the <see cref="ReplHighlighter.Highlight"/> method wraps a character literal in <c>sandybrown</c> markup,
    /// matching the colour used for string literals.
    /// </summary>
    [Fact]
    public void Highlight_CharLiteral_AppliesSandybrownMarkup()
    {
        string result = ReplHighlighter.Highlight("'G'");

        result.ShouldBe("[sandybrown]'G'[/]");
    }

    /// <summary>
    /// Tests that the <see cref="ReplHighlighter.Highlight"/> method consumes a character literal containing a tilde-escaped
    /// sequence in full, not split at the escape character.
    /// </summary>
    [Fact]
    public void Highlight_CharLiteralWithEscapeSequence_ConsumesFullLiteral()
    {
        string result = ReplHighlighter.Highlight("'~n'");

        result.ShouldBe("[sandybrown]'~n'[/]");
    }

    /// <summary>
    /// Tests that the <see cref="ReplHighlighter.Highlight"/> method wraps an integer numeric literal in <c>cornsilk1</c> markup.
    /// </summary>
    [Fact]
    public void Highlight_IntegerLiteral_AppliesCornsilk1Markup()
    {
        string result = ReplHighlighter.Highlight("42");

        result.ShouldBe("[cornsilk1]42[/]");
    }

    /// <summary>
    /// Tests that the <see cref="ReplHighlighter.Highlight"/> method wraps a floating-point numeric literal in <c>cornsilk1</c>
    /// markup.
    /// </summary>
    [Fact]
    public void Highlight_FloatLiteral_AppliesCornsilk1Markup()
    {
        string result = ReplHighlighter.Highlight("3.14");

        result.ShouldBe("[cornsilk1]3.14[/]");
    }

    /// <summary>
    /// Tests that the <see cref="ReplHighlighter.Highlight"/> method wraps a negative numeric literal in <c>cornsilk1</c> markup.
    /// </summary>
    [Fact]
    public void Highlight_NegativeIntegerLiteral_AppliesCornsilk1Markup()
    {
        string result = ReplHighlighter.Highlight("-7");
        
        result.ShouldBe("[cornsilk1]-7[/]");
    }

    /// <summary>
    /// Tests that the <see cref="ReplHighlighter.Highlight"/> method wraps <c>ASIDE:</c> and all text following it in
    /// <c>[dim]</c> markup.
    /// </summary>
    [Fact]
    public void Highlight_Comment_AppliesDimMarkupToRestOfLine()
    {
        string result = ReplHighlighter.Highlight("ASIDE: this is a comment");

        result.ShouldBe("[dim]ASIDE: this is a comment[/]");
    }

    /// <summary>
    /// Tests that the <see cref="ReplHighlighter.Highlight"/> method dims only the comment portion when a comment follows
    /// other tokens.
    /// </summary>
    [Fact]
    public void Highlight_InlineComment_DimsOnlyCommentPortion()
    {
        string result = ReplHighlighter.Highlight("BEHOLD x ASIDE: note");

        result.ShouldContain("[lightgreen_1]BEHOLD[/]");
        result.ShouldContain("[dim]ASIDE: note[/]");
    }

    /// <summary>
    /// Tests that the <see cref="ReplHighlighter.Highlight"/> method wraps <c>PEER</c> in <c>cyan</c> markup (type name category).
    /// </summary>
    [Fact]
    public void Highlight_TypeNamePeer_AppliesCyanMarkup()
    {
        string result = ReplHighlighter.Highlight("PEER");

        result.ShouldBe("[cyan]PEER[/]");
    }

    /// <summary>
    /// Tests that the <see cref="ReplHighlighter.Highlight"/> method wraps <c>VERITY</c> in <c>gold1</c> markup (boolean literal).
    /// </summary>
    [Fact]
    public void Highlight_BooleanLiteralVerity_AppliesGold1Markup()
    {
        string result = ReplHighlighter.Highlight("VERITY");

        result.ShouldBe("[gold1]VERITY[/]");
    }

    /// <summary>
    /// Tests that the <see cref="ReplHighlighter.Highlight"/> method wraps <c>NAUGHT</c> in <c>grey</c> markup (null literal).
    /// </summary>
    [Fact]
    public void Highlight_NullLiteralNaught_AppliesGreyMarkup()
    {
        string result = ReplHighlighter.Highlight("NAUGHT");

        result.ShouldBe("[grey]NAUGHT[/]");
    }

    /// <summary>
    /// Tests that the <see cref="ReplHighlighter.Highlight"/> method wraps <c>SUM OF</c> in <c>orange1</c> markup
    /// (arithmetic operator).
    /// </summary>
    [Fact]
    public void Highlight_ArithmeticOperator_AppliesOrange1Markup()
    {
        string result = ReplHighlighter.Highlight("SUM OF");

        result.ShouldBe("[orange1]SUM OF[/]");
    }

    /// <summary>
    /// Tests that the <see cref="ReplHighlighter.Highlight"/> method wraps <c>PRAY WELCOME</c> in <c>mediumpurple1</c> markup
    /// (declaration).
    /// </summary>
    [Fact]
    public void Highlight_DeclarationKeyword_AppliesMediumpurple1Markup()
    {
        string result = ReplHighlighter.Highlight("PRAY WELCOME");

        result.ShouldBe("[mediumpurple1]PRAY WELCOME[/]");
    }

    /// <summary>
    /// Tests that the <see cref="ReplHighlighter.Highlight"/> method produces markup for each keyword token
    /// in a typical single-line REPL statement in the correct colour category.
    /// </summary>
    [Fact]
    public void Highlight_TypicalStatement_ProducesMarkupForEachKeyword()
    {
        string result = ReplHighlighter.Highlight("BEHOLD SUM OF 1 AND 2");

        result.ShouldContain("[lightgreen_1]BEHOLD[/]");
        result.ShouldContain("[orange1]SUM OF[/]");
        result.ShouldContain("[cornsilk1]1[/]");
        result.ShouldContain("[orange1]AND[/]");
        result.ShouldContain("[cornsilk1]2[/]");
    }

    /// <summary>
    /// Tests that the <see cref="ReplHighlighter.Highlight"/> method matches keywords in mixed case
    /// case-insensitively but emits them in their original casing (source casing is preserved).
    /// </summary>
    [Fact]
    public void Highlight_KeywordInLowercase_MatchesAndPreservesSourceCasing()
    {
        string result = ReplHighlighter.Highlight("behold");

        result.ShouldContain("[lightgreen_1]behold[/]");
    }

}
