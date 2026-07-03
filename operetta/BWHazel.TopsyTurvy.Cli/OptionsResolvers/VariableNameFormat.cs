namespace BWHazel.TopsyTurvy.Cli.OptionsResolvers;

/// <summary>
/// Defines constants for the <c>--config varFormat</c> option, which controls how temporary variable names are generated
/// during transformation from Topsy Turvy into UtopIR.
/// </summary>
public enum VariableNameFormat
{
    /// <summary>Numeric, incrementing names, corresponds to <c>--config varFormat:numeric</c>.</summary>
    Numeric,

    /// <summary>Descriptive names built from instruction/operand details, corresponds to <c>--config varFormat:verbose</c>.</summary>
    Verbose
}
