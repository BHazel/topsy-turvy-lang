using BWHazel.TopsyTurvy.Parser;

namespace BWHazel.TopsyTurvy.Tests.Parser;

/// <summary>
/// Tests for the <see cref="VictorianFlourishPreProcessor"/> class.
/// </summary>
public class VictorianFlourishPreProcessorTests
{
    private readonly VictorianFlourishPreProcessor processor = new();

    /// <summary>
    /// Tests that <see cref="VictorianFlourishPreProcessor.Process"/> passes through source text that contains no tilde continuation markers.
    /// </summary>
    [Fact]
    public void Process_WithNoTilde_PassesTextThrough()
    {
        string input = "HARK! \"Test\"\nBEHOLD \"hello\"\nFINALE.";

        PreProcessResult result = this.processor.Process(input, new());

        result.TransformedText.ShouldContain("HARK!");
        result.TransformedText.ShouldContain("BEHOLD");
        result.TransformedText.ShouldContain("FINALE.");
    }

    /// <summary>
    /// Tests that <see cref="VictorianFlourishPreProcessor.Process"/> merges two lines when the first ends with a tilde.
    /// </summary>
    [Fact]
    public void Process_WithTrailingTilde_MergesWithNextLine()
    {
        string input = "BEHOLD~\n\"hello\"";

        PreProcessResult result = this.processor.Process(input, new());

        result.TransformedText.ShouldContain("BEHOLD\"hello\"");
    }

    /// <summary>
    /// Tests that <see cref="VictorianFlourishPreProcessor.Process"/> removes the tilde character itself when merging lines.
    /// </summary>
    [Fact]
    public void Process_WithTrailingTilde_RemovesTildeCharacter()
    {
        string input = "BEHOLD~\n\"hello\"";

        PreProcessResult result = this.processor.Process(input, new SourceMap());

        result.TransformedText.ShouldNotContain("~");
    }

    /// <summary>
    /// Tests that <see cref="VictorianFlourishPreProcessor.Process"/> merges multiple consecutive continuation lines into a single one.
    /// </summary>
    [Fact]
    public void Process_WithConsecutiveTildes_MergesAllLines()
    {
        string input = "WOVEN~\nOF~\n\"a\" AND \"b\" IF YOU PLEASE.";

        PreProcessResult result = this.processor.Process(input, new());

        result.TransformedText.ShouldContain("WOVENOF\"a\" AND \"b\" IF YOU PLEASE.");
    }

    /// <summary>
    /// Tests that <see cref="VictorianFlourishPreProcessor.Process"/> does not treat a tilde in the middle of a line as a continuation marker.
    /// </summary>
    [Fact]
    public void Process_WithTildeMidLine_NotTreatedAsContinuation()
    {
        string input = "BEH~OLD \"hello\"\nFINALE.";

        PreProcessResult result = this.processor.Process(input, new());

        result.TransformedText.ShouldContain("BEH~OLD");
        result.TransformedText.ShouldContain("FINALE.");
    }

    /// <summary>
    /// Tests that <see cref="VictorianFlourishPreProcessor.Process"/> adds mappings to the source map for each line.
    /// </summary>
    [Fact]
    public void Process_WithMultipleLines_PopulatesSourceMap()
    {
        SourceMap map = new();
        string input = "line one\nline two";

        this.processor.Process(input, map);

        (int line, int column) = map.GetOriginalLocation(0);
        line.ShouldBe(1);
        column.ShouldBe(1);
    }
}
