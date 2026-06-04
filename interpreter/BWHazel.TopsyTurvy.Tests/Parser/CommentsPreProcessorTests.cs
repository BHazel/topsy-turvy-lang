using BWHazel.TopsyTurvy.Parser;

namespace BWHazel.TopsyTurvy.Tests.Parser;

/// <summary>
/// Tests for the <see cref="CommentsPreProcessor"/> class.
/// </summary>
public class CommentsPreProcessorTests
{
    private readonly CommentsPreProcessor processor = new();

    /// <summary>
    /// Tests that <see cref="CommentsPreProcessor.Process"/> passes through source text that contains no comments.
    /// </summary>
    [Fact]
    public void Process_WithNoComments_ReturnsTextUnchanged()
    {
        string input = "HARK! \"Test\" BEHOLD \"hello\" FINALE.";

        PreProcessResult result = this.processor.Process(input, new());

        result.Text.ShouldBe(input);
    }

    /// <summary>
    /// Tests that <see cref="CommentsPreProcessor.Process"/> removes a line comment entirely.
    /// </summary>
    [Fact]
    public void Process_WithLineComment_StripsCommentText()
    {
        string input = "BEHOLD x ASIDE: this is a comment";

        PreProcessResult result = this.processor.Process(input, new());

        result.Text.ShouldNotContain("ASIDE:");
        result.Text.ShouldNotContain("this is a comment");
    }

    /// <summary>
    /// Tests that <see cref="CommentsPreProcessor.Process"/> preserves the code that precedes a line comment.
    /// </summary>
    [Fact]
    public void Process_WithLineCommentMidLine_PreservesCodeBeforeComment()
    {
        string input = "BEHOLD x ASIDE: this is a comment";

        PreProcessResult result = this.processor.Process(input, new());

        result.Text.ShouldStartWith("BEHOLD x");
    }

    /// <summary>
    /// Tests that <see cref="CommentsPreProcessor.Process"/> removes a block comment content.
    /// </summary>
    [Fact]
    public void Process_WithSingleLineBlockComment_StripsCommentText()
    {
        string input = "(ASIDE, AT SOME LENGTH: some text END OF ASIDE.)";

        PreProcessResult result = this.processor.Process(input, new());
        
        result.Text.ShouldNotContain("some text");
    }

    /// <summary>
    /// Tests that <see cref="CommentsPreProcessor.Process"/> preserves new lines inside a block comment.
    /// </summary>
    [Fact]
    public void Process_WithMultiLineBlockComment_PreservesNewlines()
    {
        string input = "(ASIDE, AT SOME LENGTH:\nfirst line\nsecond line\nEND OF ASIDE.)";

        PreProcessResult result = this.processor.Process(input, new());

        int newLineCount = 0;
        foreach (char character in result.Text)
        {
            if (character == '\n')
            {
                newLineCount++;
            }
        }

        newLineCount.ShouldBe(3);
    }

    /// <summary>
    /// Tests that <see cref="CommentsPreProcessor.Process"/> removes multiple line comments on different lines.
    /// </summary>
    [Fact]
    public void Process_WithMultipleLineComments_StripsAllComments()
    {
        string input = "BEHOLD x ASIDE: first comment\nBEHOLD y ASIDE: second comment";

        PreProcessResult result = this.processor.Process(input, new());

        result.Text.ShouldNotContain("first comment");
        result.Text.ShouldNotContain("second comment");
    }

    /// <summary>
    /// Tests that <see cref="CommentsPreProcessor.Process"/> strips a block comment that appears inline between code tokens.
    /// </summary>
    [Fact]
    public void Process_WithInlineBlockComment_StripsComment()
    {
        string input = "BEHOLD  x";

        PreProcessResult result = this.processor.Process(input, new());

        result.Text.ShouldNotContain("(ASIDE, AT SOME LENGTH: ignored END OF ASIDE.)");
        result.Text.ShouldContain("BEHOLD");
        result.Text.ShouldContain("x");
    }
}
