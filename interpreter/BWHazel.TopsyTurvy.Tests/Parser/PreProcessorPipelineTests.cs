using BWHazel.TopsyTurvy.Parser;

namespace BWHazel.TopsyTurvy.Tests.Parser;

/// <summary>
/// Tests for the <see cref="PreProcessorPipeline"/> class.
/// </summary>
public class PreProcessorPipelineTests
{
    /// <summary>
    /// Tests that the <see cref="PreProcessorPipeline.Execute"/> method returns the original input text unchanged when no processors are registered.
    /// </summary>
    [Fact]
    public void Execute_WithNoProcessors_ReturnsInputTextUnchanged()
    {
        PreProcessorPipeline pipeline = new();
        string input = "HARK! \"Test\" FINALE.";

        PreProcessResult result = pipeline.Execute(input);

        result.Text.ShouldBe(input);
    }

    /// <summary>
    /// Tests that the <see cref="PreProcessorPipeline.Execute"/> method returns a source map that resolves any offset to line 1, column 1 when no processors are registered.
    /// </summary>
    [Fact]
    public void Execute_WithNoProcessors_ReturnsEmptySourceMap()
    {
        PreProcessorPipeline pipeline = new();

        PreProcessResult result = pipeline.Execute("any input");

        (int line, int column) = result.SourceMap.GetOriginalLocation(0);
        line.ShouldBe(1);
        column.ShouldBe(1);
    }

    /// <summary>
    /// Tests that the <see cref="PreProcessorPipeline.Execute"/> method applies a single registered processor text transformation.
    /// </summary>
    [Fact]
    public void Execute_WithSingleProcessor_AppliesTransformation()
    {
        PreProcessorPipeline pipeline = new();
        pipeline.AddProcessor(new TestPrefixingPreProcessor(">>"));

        PreProcessResult result = pipeline.Execute("hello");

        result.Text.ShouldBe(">>hello");
    }

    /// <summary>
    /// Tests that the <see cref="PreProcessorPipeline.Execute"/> method applies processors in registration order, with the first processor output becoming the second processor input.
    /// </summary>
    [Fact]
    public void Execute_WithTwoProcessors_AppliesFirstThenSecond()
    {
        PreProcessorPipeline pipeline = new();
        pipeline.AddProcessor(new TestPrefixingPreProcessor("A"));
        pipeline.AddProcessor(new TestPrefixingPreProcessor("B"));

        PreProcessResult result = pipeline.Execute("X");

        result.Text.ShouldBe("BAX");
    }

    /// <summary>
    /// Tests that the <see cref="PreProcessorPipeline.Execute"/> method produces a different result when the same processors are registered in reverse order.
    /// </summary>
    [Fact]
    public void Execute_WithProcessorsInReversedOrder_ProducesDifferentResult()
    {
        PreProcessorPipeline pipeline = new();
        pipeline.AddProcessor(new TestPrefixingPreProcessor("B"));
        pipeline.AddProcessor(new TestPrefixingPreProcessor("A"));

        PreProcessResult result = pipeline.Execute("X");

        result.Text.ShouldBe("ABX");
    }

    /// <summary>
    /// Tests that the <see cref="PreProcessorPipeline.Execute"/> method accumulates source map entries from all processors into the returned source map.
    /// </summary>
    [Fact]
    public void Execute_WithMultipleProcessorsThatAddMappings_AccumulatesMappingsFromAll()
    {
        PreProcessorPipeline pipeline = new();
        pipeline.AddProcessor(new TestMappingAddingPreProcessor(offset: 0, line: 1, column: 1));
        pipeline.AddProcessor(new TestMappingAddingPreProcessor(offset: 5, line: 2, column: 1));

        PreProcessResult result = pipeline.Execute("hello world");

        (int line1, _) = result.SourceMap.GetOriginalLocation(0);
        (int line2, _) = result.SourceMap.GetOriginalLocation(5);
        line1.ShouldBe(1);
        line2.ShouldBe(2);
    }

    /// <summary>
    /// Tests that the <see cref="PreProcessorPipeline.Execute"/> method passes the same <see cref="SourceMap"/> instance to every processor ensuring that map mutations are shared across the pipeline.
    /// </summary>
    [Fact]
    public void Execute_WithMultipleProcessors_PassesSameSourceMapInstanceToAll()
    {
        PreProcessorPipeline pipeline = new();
        TestSourceMapCapturingPreProcessor first = new();
        TestSourceMapCapturingPreProcessor second = new();
        pipeline.AddProcessor(first);
        pipeline.AddProcessor(second);

        pipeline.Execute("input");

        second.CapturedMap.ShouldBeSameAs(first.CapturedMap);
    }
}
