using System.Collections.Generic;
using BWHazel.TopsyTurvy.StandardLibrary;
using BWHazel.TopsyTurvy.Tests.Runtime;
using Shouldly;

namespace BWHazel.TopsyTurvy.Tests.StandardLibrary;

/// <summary>
/// Tests for the <see cref="Global"/> class.
/// </summary>
public class GlobalTests
{
    /// <summary>
    /// Tests that <see cref="Global.PreviewBehold"/> suppresses the trailing newline when <c>withCeremony</c> is <c>false</c>,
    /// applying the <c>suppressNewline</c> polarity inversion.
    /// </summary>
    [Fact]
    public void PreviewBehold_WithoutCeremony_SuppressesNewline()
    {
        TestIO io = new([], new Queue<string>());

        Global.PreviewBehold("Hello", withCeremony: false, io);

        io.Writes.ShouldHaveSingleItem();
        io.Writes[0].Message.ShouldBe("Hello");
        io.Writes[0].SuppressNewline.ShouldBeTrue();
    }

    /// <summary>
    /// Tests that <see cref="Global.PreviewBehold"/> applies the trailing newline when <c>withCeremony</c> is <c>true</c>,
    /// applying the <c>suppressNewline</c> polarity inversion.
    /// </summary>
    [Fact]
    public void PreviewBehold_WithCeremony_AppliesNewline()
    {
        TestIO io = new([], new Queue<string>());

        Global.PreviewBehold("Hello", withCeremony: true, io);

        io.Writes.ShouldHaveSingleItem();
        io.Writes[0].Message.ShouldBe("Hello");
        io.Writes[0].SuppressNewline.ShouldBeFalse();
    }

    /// <summary>
    /// Tests that <see cref="Global.PreviewPrayTell"/> returns the next line from the input source.
    /// </summary>
    [Fact]
    public void PreviewPrayTell_WithQueuedInput_ReturnsNextLine()
    {
        TestIO io = new([], new Queue<string>(["A Most Ingenious Paradox!"]));

        string result = Global.PreviewPrayTell(io);

        result.ShouldBe("A Most Ingenious Paradox!");
    }
}
