namespace BWHazel.TopsyTurvy.UtopIR.Transformer.VariableNameFormatters;

/// <summary>
/// Defines methods for creating names for temporary virtual registers <see cref="TopsyTurvyToUtopIRTransformer"/>
/// generates when transforming Topsy Turvy code into UtopIR.
/// </summary>
/// <remarks>
/// Implementing this interface provides the ability to customise how temporary virtual registers are named in transformed UtopIR
/// code.  This can be useful for debugging and testing, where detailed information would be beneficial, or for obfuscation or
/// optimisation where less information is desired.
/// </remarks>
public interface ITemporaryVariableNameFormatter
{
    /// <summary>
    /// Creates a name for a new temporary virtual register.
    /// </summary>
    /// <param name="parts">The raw UtopIR instruction information describing what the register holds.</param>
    /// <returns>A temporary variable name, without the <c>£</c> character.</returns>
    string CreateName(params string[] parts);
}
