using System.Collections.Generic;
using BWHazel.TopsyTurvy.Bindings;
using BWHazel.TopsyTurvy.UtopIR.Emitters.Cil;

namespace BWHazel.TopsyTurvy.Cli.OptionsResolvers;

/// <summary>
/// Options for the emit and target compilation operations for .NET CIL.
/// </summary>
/// <param name="EmitOptions">The options passed to <see cref="CilEmitter"/>.</param>
/// <param name="Format">The temporary variable naming scheme to use when transforming Topsy Turvy source.</param>
/// <param name="ExternalFunctions">The catalogue passed to the type checker and UtopIR transformer, or <c>null</c> to use only the Standard Library.</param>
/// <param name="ExternalLibraryAssemblyPaths">The resolved paths of admitted external library assemblies to copy alongside a built executable, or <c>null</c> for none.</param>
/// <remarks>
/// <see cref="ExternalFunctions"/> is the raw catalogue consumed before CIL emission (type-checking, UtopIR
/// transformation), distinct from <see cref="CilEmitOptions.ExternalFunctions"/>, which is the same catalogue
/// already projected into the reflection-only shapes the CIL emitter itself needs.
/// </remarks>
public sealed record CilTargetOptions(
    CilEmitOptions EmitOptions,
    VariableNameFormat Format = VariableNameFormat.Numeric,
    BindingCatalogue? ExternalFunctions = null,
    IReadOnlyList<string>? ExternalLibraryAssemblyPaths = null);
