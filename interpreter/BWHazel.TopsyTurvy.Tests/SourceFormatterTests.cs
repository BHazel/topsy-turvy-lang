using BWHazel.TopsyTurvy.Analysis;

namespace BWHazel.TopsyTurvy.Tests;

/// <summary>
/// Tests for the <see cref="SourceFormatter"/> class.
/// </summary>
public class SourceFormatterTests
{
    /// <summary>
    /// Tests that the <see cref="SourceFormatter.FormatSource"/> method return an empty string when given an empty source.
    /// </summary>
    [Fact]
    public void FormatSource_WithEmptySource_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, SourceFormatter.FormatSource(string.Empty));
    }

    /// <summary>
    /// Tests that the <see cref="SourceFormatter.FormatSource"/> method returns an already-formatted program unchanged.
    /// </summary>
    [Fact]
    public void FormatSource_WithAlreadyFormattedProgram_ReturnsSourceUnchanged()
    {
        string source = "HARK! \"Test\"\nFINALE.";

        Assert.Equal(source, SourceFormatter.FormatSource(source));
    }

    /// <summary>
    /// Tests that the <see cref="SourceFormatter.FormatSource"/> method preserves blank lines.
    /// </summary>
    [Fact]
    public void FormatSource_WithBlankLines_PreservesBlankLines()
    {
        string source = "HARK! \"Test\"\n\nFINALE.";

        Assert.Equal(source, SourceFormatter.FormatSource(source));
    }

    /// <summary>
    /// Tests that the <see cref="SourceFormatter.FormatSource"/> method indents delarations inside a PRINCIPALS block by one level.
    /// </summary>
    [Fact]
    public void FormatSource_WithPrincipalsBlock_IndentsDeclarations()
    {
        string input = "HARK! \"Test\"\nPRINCIPALS\nPRAY WELCOME x AS A PEER BEING 0\nTHE CURTAIN RISES.\nFINALE.";
        string expected = "HARK! \"Test\"\nPRINCIPALS\n  PRAY WELCOME x AS A PEER BEING 0\nTHE CURTAIN RISES.\nFINALE.";

        Assert.Equal(expected, SourceFormatter.FormatSource(input));
    }

    /// <summary>
    /// Tests that the <see cref="SourceFormatter.FormatSource"/> method indents a function body by one level and returns to the outer level after the closing keyword.
    /// </summary>
    [Fact]
    public void FormatSource_WithFunctionBlock_IndentsBody()
    {
        string input = "HARK! \"Test\"\nIT IS MY DUTY TO PERFORM greet UNDER NO OBLIGATION\nBEHOLD \"hello\"\nMY DUTY IS DISCHARGED.\nFINALE.";
        string expected = "HARK! \"Test\"\nIT IS MY DUTY TO PERFORM greet UNDER NO OBLIGATION\n  BEHOLD \"hello\"\nMY DUTY IS DISCHARGED.\nFINALE.";

        Assert.Equal(expected, SourceFormatter.FormatSource(input));
    }

    /// <summary>
    /// Tests that the <see cref="SourceFormatter.FormatSource"/> method indents a conditional <c>true</c> block by one level and double-indents its body.
    /// </summary>
    [Fact]
    public void FormatSource_WithIfBlock_IndentsCorrectly()
    {
        string input = "HARK! \"Test\"\nSHOULD IT TRANSPIRE THAT VERITY\nQUITE SO.\nBEHOLD \"yes\"\nSO MUCH FOR THAT.\nFINALE.";
        string expected = "HARK! \"Test\"\nSHOULD IT TRANSPIRE THAT VERITY\n  QUITE SO.\n    BEHOLD \"yes\"\nSO MUCH FOR THAT.\nFINALE.";
        
        Assert.Equal(expected, SourceFormatter.FormatSource(input));
    }

    /// <summary>
    /// Tests that the <see cref="SourceFormatter.FormatSource"/> method decreases a <c>false</c> block depth (mid-block keyword) by one level then increases it again.
    /// </summary>
    [Fact]
    public void FormatSource_WithElseBlock_IndentsOtherwiseMidBlock()
    {
        string input = "HARK! \"Test\"\nSHOULD IT TRANSPIRE THAT VERITY\nQUITE SO.\nBEHOLD \"yes\"\nOTHERWISE,\nBEHOLD \"no\"\nSO MUCH FOR THAT.\nFINALE.";
        string expected = "HARK! \"Test\"\nSHOULD IT TRANSPIRE THAT VERITY\n  QUITE SO.\n    BEHOLD \"yes\"\n  OTHERWISE,\n    BEHOLD \"no\"\nSO MUCH FOR THAT.\nFINALE.";

        Assert.Equal(expected, SourceFormatter.FormatSource(input));
    }

    /// <summary>
    /// Tests that the <see cref="SourceFormatter.FormatSource"/> method increases depth by 2 for a Switch block and treats its Case keyword as a mid-block keyword.
    /// </summary>
    [Fact]
    public void FormatSource_WithSwitchBlock_IndentsWithDoubleDepth()
    {
        string input = "HARK! \"Test\"\nIN WHICH CAPACITY?\nWHEN ACTING AS 1\nBEHOLD \"one\"\nNOTHING COULD BE MORE SATISFACTORY.\nFINALE.";
        string expected = "HARK! \"Test\"\nIN WHICH CAPACITY?\n  WHEN ACTING AS 1\n    BEHOLD \"one\"\nNOTHING COULD BE MORE SATISFACTORY.\nFINALE.";

        Assert.Equal(expected, SourceFormatter.FormatSource(input));
    }

    /// <summary>
    /// Tests that the <see cref="SourceFormatter.FormatSource"/> method indents a loop body.
    /// </summary>
    [Fact]
    public void FormatSource_WithLoopBlock_IndentsBody()
    {
        string input = "HARK! \"Test\"\nBY A LEGAL FICTION\nBEHOLD \"loop\"\nTHE TERM EXPIRES.\nFINALE.";
        string expected = "HARK! \"Test\"\nBY A LEGAL FICTION\n  BEHOLD \"loop\"\nTHE TERM EXPIRES.\nFINALE.";

        Assert.Equal(expected, SourceFormatter.FormatSource(input));
    }

    /// <summary>
    /// Tests that the <see cref="SourceFormatter.FormatSource"/> method indents a Try/Catch block and applies double indentation for Try and Catch bodies.
    /// </summary>
    [Fact]
    public void FormatSource_WithTryCatchBlock_IndentsCorrectly()
    {
        string input = "HARK! \"Test\"\nWITH THE GREATEST RESPECT,\nBEHOLD \"try\"\nWITH GRATITUDE\nBEHOLD \"success\"\nMODIFIED RAPTURE\nBEHOLD \"error\"\nTHAT CONCLUDES THE MATTER.\nFINALE.";
        string expected = "HARK! \"Test\"\nWITH THE GREATEST RESPECT,\n  BEHOLD \"try\"\nWITH GRATITUDE\n  BEHOLD \"success\"\nMODIFIED RAPTURE\n  BEHOLD \"error\"\nTHAT CONCLUDES THE MATTER.\nFINALE.";

        Assert.Equal(expected, SourceFormatter.FormatSource(input));
    }

    /// <summary>
    /// Tests that the <see cref="SourceFormatter.FormatSource"/> method corrects over-indented input to the canonical depth.
    /// </summary>
    [Fact]
    public void FormatSource_WithOverIndentedInput_ReIndentsCorrectly()
    {
        string input = "HARK! \"Test\"\nPRINCIPALS\n          PRAY WELCOME x AS A PEER BEING 0\nTHE CURTAIN RISES.\nFINALE.";
        string expected = "HARK! \"Test\"\nPRINCIPALS\n  PRAY WELCOME x AS A PEER BEING 0\nTHE CURTAIN RISES.\nFINALE.";

        Assert.Equal(expected, SourceFormatter.FormatSource(input));
    }

    /// <summary>
    /// Tests that the <see cref="SourceFormatter.FormatSource"/> method indents the subtitle line to 2 spaces.
    /// </summary>
    [Fact]
    public void FormatSource_WithSubtitleLine_AlwaysIndentsByTwoSpaces()
    {
        string input = "HARK! \"Title\"\nor, \"Subtitle\"\nFINALE.";
        string expected = "HARK! \"Title\"\n  or, \"Subtitle\"\nFINALE.";
        Assert.Equal(expected, SourceFormatter.FormatSource(input));
    }

    /// <summary>
    /// Tests that the <see cref="SourceFormatter.FormatSource"/> method converts a lower-case keyword to upper-case.
    /// </summary>
    [Fact]
    public void FormatSource_WithLowercaseKeyword_NormalisesToCanonicalCase()
    {
        string input = "HARK! \"Test\"\nbehold \"hello\"\nFINALE.";
        string expected = "HARK! \"Test\"\nBEHOLD \"hello\"\nFINALE.";

        Assert.Equal(expected, SourceFormatter.FormatSource(input));
    }

    /// <summary>
    /// Tests that the <see cref="SourceFormatter.FormatSource"/> method converts a keyword with mixed case to upper-case.
    /// </summary>
    [Fact]
    public void FormatSource_WithMixedCaseKeyword_NormalisesToCanonicalCase()
    {
        string input = "HARK! \"Test\"\nsum of 1 AND 2\nFINALE.";
        string expected = "HARK! \"Test\"\nSUM OF 1 AND 2\nFINALE.";

        Assert.Equal(expected, SourceFormatter.FormatSource(input));
    }

    /// <summary>
    /// Tests that the <see cref="SourceFormatter.FormatSource"/> method does not convert case for a keyword inside a string literal.
    /// </summary>
    [Fact]
    public void FormatSource_WithKeywordInsideStringLiteral_NotNormalised()
    {
        string source = "HARK! \"Test\"\nBEHOLD \"behold this\"\nFINALE.";
        Assert.Equal(source, SourceFormatter.FormatSource(source));
    }

    /// <summary>
    /// Tests that the <see cref="SourceFormatter.FormatSource"/> method does not convert case for a keyword inside a line comment.
    /// </summary>
    [Fact]
    public void FormatSource_WithKeywordInsideLineComment_NotNormalised()
    {
        string source = "HARK! \"Test\"\nBEHOLD \"hello\" ASIDE: behold this\nFINALE.";

        Assert.Equal(source, SourceFormatter.FormatSource(source));
    }

    /// <summary>
    /// Tests that the <see cref="SourceFormatter.FormatSource"/> method writes a single-line block comment verbatim without formatting.
    /// </summary>
    [Fact]
    public void FormatSource_WithSingleLineBlockComment_WrittenVerbatim()
    {
        string source = "HARK! \"Test\"\n(ASIDE, AT SOME LENGTH: behold this END OF ASIDE.)\nFINALE.";

        Assert.Equal(source, SourceFormatter.FormatSource(source));
    }

    /// <summary>
    /// Tests that the <see cref="SourceFormatter.FormatSource"/> method writes a multi-line block comment verbatim without formatting.
    /// </summary>
    [Fact]
    public void FormatSource_WithMultiLineBlockComment_AllLinesWrittenVerbatim()
    {
        string source = "HARK! \"Test\"\n(ASIDE, AT SOME LENGTH:\n  behold this\n  should it transpire\nEND OF ASIDE.)\nFINALE.";
        
        Assert.Equal(source, SourceFormatter.FormatSource(source));
    }
}
