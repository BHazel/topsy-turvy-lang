namespace BWHazel.TopsyTurvy.UtopIR.Transformer.VariableNameFormatters;

/// <summary>
/// Names temporary virtual registers with a simple incrementing integer, starting at 0 and
/// ignoring the instruction and operand details entirely.
/// </summary>
/// <remarks>
/// <para>
/// Variable names are short and collision-free by design as each name is only ever produced once per formatter during the
/// transformation of a single Topsy Turvy program into UtopIR.  However, the names no longer describe what their variables
/// hold, although allow for more compact and navigable code.
/// </para>
/// <para>
/// Example names produced by this formatter are:
/// * <c>_0</c>
/// * <c>_1</c>
/// * <c>_2</c>
/// </para>
/// </remarks>
public sealed class IncrementingIntVariableFormatter : ITemporaryVariableNameFormatter
{
    private int counter;

    /// <inheritdoc />
    public string CreateName(string mnemonic, params string[] operandParts) => $"_{this.counter++}";
}
