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

        Assert.Equal(input, result.Text);
    }

    /// <summary>
    /// Tests that <see cref="CommentsPreProcessor.Process"/> removes a line comment entirely.
    /// </summary>
    [Fact]
    public void Process_WithLineComment_StripsCommentText()
    {
        string input = "BEHOLD x ASIDE: this is a comment";

        PreProcessResult result = this.processor.Process(input, new());

        Assert.DoesNotContain("ASIDE:", result.Text);
        Assert.DoesNotContain("this is a comment", result.Text);
    }

    /// <summary>
    /// Tests that <see cref="CommentsPreProcessor.Process"/> preserves the code that precedes a line comment.
    /// </summary>
    [Fact]
    public void Process_WithLineCommentMidLine_PreservesCodeBeforeComment()
    {
        string input = "BEHOLD x ASIDE: this is a comment";

        PreProcessResult result = this.processor.Process(input, new());

        Assert.StartsWith("BEHOLD x", result.Text);
    }

    /// <summary>
    /// Tests that <see cref="CommentsPreProcessor.Process"/> removes a block comment content.
    /// </summary>
    [Fact]
    public void Process_WithSingleLineBlockComment_StripsCommentText()
    {
        string input = "(ASIDE, AT SOME LENGTH: some text END OF ASIDE.)";

        PreProcessResult result = this.processor.Process(input, new());
        
        Assert.DoesNotContain("some text", result.Text);
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

        Assert.Equal(3, newLineCount);
    }

    /// <summary>
    /// Tests that <see cref="CommentsPreProcessor.Process"/> removes multiple line comments on different lines.
    /// </summary>
    [Fact]
    public void Process_WithMultipleLineComments_StripsAllComments()
    {
        string input = "BEHOLD x ASIDE: first comment\nBEHOLD y ASIDE: second comment";

        PreProcessResult result = this.processor.Process(input, new());

        Assert.DoesNotContain("first comment", result.Text);
        Assert.DoesNotContain("second comment", result.Text);
    }

    /// <summary>
    /// Tests that <see cref="CommentsPreProcessor.Process"/> strips a block comment that appears inline between code tokens.
    /// </summary>
    [Fact]
    public void Process_WithInlineBlockComment_StripsComment()
    {
        string input = "BEHOLD  x";

        PreProcessResult result = this.processor.Process(input, new());

        Assert.DoesNotContain("(ASIDE, AT SOME LENGTH: ignored END OF ASIDE.)", result.Text);
        Assert.Contains("BEHOLD", result.Text);
        Assert.Contains("x", result.Text);
    }
}
