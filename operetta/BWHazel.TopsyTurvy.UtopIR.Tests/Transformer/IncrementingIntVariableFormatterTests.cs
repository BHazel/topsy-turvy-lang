using BWHazel.TopsyTurvy.UtopIR.Transformer.VariableNameFormatters;

namespace BWHazel.TopsyTurvy.UtopIR.Tests.Transformer;

/// <summary>
/// Tests for <see cref="IncrementingIntVariableFormatter"/>.
/// </summary>
public class IncrementingIntVariableFormatterTests
{
    /// <summary>
    /// Tests that <see cref="IncrementingIntVariableFormatter.CreateName(string, string[])"/> returns names starting at <c>_0</c> and incrementing by one, ignoring the supplied mnemonic and operand parts.
    /// </summary>
    [Fact]
    public void CreateName_WithSuccessiveCalls_ReturnsIncrementingNamesIgnoringParts()
    {
        IncrementingIntVariableFormatter formatter = new();

        string first = formatter.CreateName("sum", "a", "b");
        string second = formatter.CreateName("were", "x", "chancellor");
        string third = formatter.CreateName("leave");

        first.ShouldBe("_0");
        second.ShouldBe("_1");
        third.ShouldBe("_2");
    }
}
