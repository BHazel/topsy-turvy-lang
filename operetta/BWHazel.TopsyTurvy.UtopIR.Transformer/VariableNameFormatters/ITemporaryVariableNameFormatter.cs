namespace BWHazel.TopsyTurvy.UtopIR.Transformer.VariableNameFormatters;

/// <summary>
/// Defines methods for creating names for temporary virtual registers <see cref="TopsyTurvyToUtopIRTransformer"/>
/// generates when transforming Topsy Turvy code into UtopIR.
/// </summary>
/// <remarks>
/// Implementing this interface provides the ability to customise how temporary virtual registers are named in transformed UtopIR
/// code.  This can be useful for debugging and testing, where detailed information would be beneficial, or for obfuscation or
/// optimisation where less information is desired.  The instruction mnemonic is always supplied separately from the operand
/// parts, since a mnemonic formatting rules can legitimately differ from an operand.
/// </remarks>
public interface ITemporaryVariableNameFormatter
{
    /// <summary>
    /// Creates a name for a new temporary virtual register.
    /// </summary>
    /// <param name="mnemonic">The UtopIR instruction mnemonic the register is the target of.</param>
    /// <param name="operandParts">The raw UtopIR operand values or names describing what the register holds.</param>
    /// <returns>A temporary variable name, without the <c>£</c> character.</returns>
    string CreateName(string mnemonic, params string[] operandParts);
}
