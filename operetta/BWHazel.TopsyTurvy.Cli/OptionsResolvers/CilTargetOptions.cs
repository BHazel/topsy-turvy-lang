using BWHazel.TopsyTurvy.UtopIR.Emitters.Cil;

namespace BWHazel.TopsyTurvy.Cli.OptionsResolvers;

/// <summary>
/// Options for the emit and target compilation operations for .NET CIL.
/// </summary>
/// <param name="EmitOptions">The options passed to <see cref="CilEmitter"/>.</param>
/// <param name="Format">The temporary variable naming scheme to use when transforming Topsy Turvy source.</param>
public sealed record CilTargetOptions(CilEmitOptions EmitOptions, VariableNameFormat Format = VariableNameFormat.Numeric);
