using System.Collections.Generic;
using BWHazel.TopsyTurvy.WebEditor.IO;

namespace BWHazel.TopsyTurvy.WebEditor.Tests.IO;

/// <summary>
/// Tests for the <see cref="BufferedWebIO"/> class.
/// </summary>
public class BufferedWebIOTests
{
    /// <summary>
    /// Tests that <see cref="BufferedWebIO.ReadLine"/> dequeues supplied input lines in order.
    /// </summary>
    [Fact]
    public void ReadLine_DequeuesSuppliedInputInOrder()
    {
        BufferedWebIO io = new(["first", "second", "third"]);

        io.ReadLine().ShouldBe("first");
        io.ReadLine().ShouldBe("second");
        io.ReadLine().ShouldBe("third");
    }

    /// <summary>
    /// Tests that <see cref="BufferedWebIO.ReadLine"/> returns an empty string, not an exception,
    /// once the input queue is exhausted.
    /// </summary>
    [Fact]
    public void ReadLine_WhenQueueExhausted_ReturnsEmptyString()
    {
        BufferedWebIO io = new(["only"]);
        io.ReadLine();

        io.ReadLine().ShouldBe(string.Empty);
    }

    /// <summary>
    /// Tests that <see cref="BufferedWebIO.WriteLine"/> appends a new output line by default.
    /// </summary>
    [Fact]
    public void WriteLine_AppendsNewOutputLineByDefault()
    {
        BufferedWebIO io = new([]);

        io.WriteLine("first");
        io.WriteLine("second");

        io.OutputLines.ShouldBe(["first", "second"]);
    }

    /// <summary>
    /// Tests that <see cref="BufferedWebIO.WriteLine"/> with <c>suppressNewline</c> set to <c>true</c> concatenates
    /// onto the last output line instead of starting a new one.
    /// </summary>
    [Fact]
    public void WriteLine_WithSuppressNewlineTrue_ConcatenatesOntoLastOutputLine()
    {
        BufferedWebIO io = new([]);
        io.WriteLine("Hello, ");

        io.WriteLine("World!", suppressNewline: true);

        io.OutputLines.ShouldBe(["Hello, World!"]);
    }

    /// <summary>
    /// Tests that <c>suppressNewline</c> set to <c>true</c> on the very first write still starts a new line,
    /// since there is no prior output line to concatenate onto.
    /// </summary>
    [Fact]
    public void WriteLine_WithSuppressNewlineTrueButNoPriorOutput_StartsNewLine()
    {
        BufferedWebIO io = new([]);

        io.WriteLine("first", suppressNewline: true);

        io.OutputLines.ShouldBe(["first"]);
    }

    /// <summary>
    /// Tests that <see cref="BufferedWebIO.OutputLines"/> reflects all writes in order.
    /// </summary>
    [Fact]
    public void OutputLines_ReflectsAllWritesInOrder()
    {
        BufferedWebIO io = new([]);

        io.WriteLine("one");
        io.WriteLine("two");
        io.WriteLine("three");

        io.OutputLines.Count.ShouldBe(3);
        io.OutputLines[0].ShouldBe("one");
        io.OutputLines[1].ShouldBe("two");
        io.OutputLines[2].ShouldBe("three");
    }
}
