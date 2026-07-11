using BWHazel.TopsyTurvy.UtopIR.Transformer.VariableNameFormatters;

namespace BWHazel.TopsyTurvy.UtopIR.Tests.Transformer;

/// <summary>
/// Tests for <see cref="InstructionDetailVariableFormatter"/>.
/// </summary>
public class InstructionDetailVariableFormatterTests
{
    /// <summary>
    /// Tests that <see cref="InstructionDetailVariableFormatter.CreateName(string, string[])"/> joins parts with an underscore, prefixed with an underscore.
    /// </summary>
    [Fact]
    public void CreateName_WithMultipleParts_JoinsWithUnderscoresAndPrefix()
    {
        InstructionDetailVariableFormatter formatter = new();

        string name = formatter.CreateName("sum", "a", "b");

        name.ShouldBe("_sum_a_b");
    }

    /// <summary>
    /// Tests that <see cref="InstructionDetailVariableFormatter.CreateName(string, string[])"/> replaces a decimal point in an operand part with <c>p</c>.
    /// </summary>
    [Fact]
    public void CreateName_WithDecimalPointInOperand_ReplacesWithP()
    {
        InstructionDetailVariableFormatter formatter = new();

        string name = formatter.CreateName("sum", "3.14", "b");

        name.ShouldBe("_sum_3p14_b");
    }

    /// <summary>
    /// Tests that <see cref="InstructionDetailVariableFormatter.CreateName(string, string[])"/> drops the <c>.</c> in a <c>.f</c>-suffixed mnemonic entirely, rather than replacing it with <c>p</c> as it would for an operand decimal point.
    /// </summary>
    [Fact]
    public void CreateName_WithDotInMnemonic_DropsDot()
    {
        InstructionDetailVariableFormatter formatter = new();

        string name = formatter.CreateName("sum.f", "a", "b");

        name.ShouldBe("_sumf_a_b");
    }

    /// <summary>
    /// Tests that <see cref="InstructionDetailVariableFormatter.CreateName(string, string[])"/> replaces punctuation other than hyphen/underscore with a hyphen.
    /// </summary>
    [Fact]
    public void CreateName_WithOtherPunctuation_ReplacesWithHyphen()
    {
        InstructionDetailVariableFormatter formatter = new();

        string name = formatter.CreateName("sum", "\"hi!\"", "b");

        name.ShouldBe("_sum_-hi--_b");
    }

    /// <summary>
    /// Tests that <see cref="InstructionDetailVariableFormatter.CreateName(string, string[])"/> preserves hyphens and underscores in parts unchanged.
    /// </summary>
    [Fact]
    public void CreateName_WithHyphenAndUnderscore_PreservesUnchanged()
    {
        InstructionDetailVariableFormatter formatter = new();

        string name = formatter.CreateName("sum", "-7", "my_var");

        name.ShouldBe("_sum_-7_my_var");
    }

    /// <summary>
    /// Tests that <see cref="InstructionDetailVariableFormatter.CreateName(string, string[])"/> appends a numeric suffix when the same composed name has already been produced.
    /// </summary>
    [Fact]
    public void CreateName_WithDuplicateComposedName_AppendsIncrementingSuffix()
    {
        InstructionDetailVariableFormatter formatter = new();

        string first = formatter.CreateName("sum", "a", "b");
        string second = formatter.CreateName("sum", "a", "b");
        string third = formatter.CreateName("sum", "a", "b");

        first.ShouldBe("_sum_a_b");
        second.ShouldBe("_sum_a_b_2");
        third.ShouldBe("_sum_a_b_3");
    }
}
