using System.Collections.Generic;

namespace BWHazel.TopsyTurvy.UtopIR.Emitters.Cil;

/// <summary>
/// Options controlling how the <see cref="CilEmitter"/> produces a .NET assembly.
/// </summary>
/// <param name="AssemblyName">The name of the assembly used as the CLR assembly identity and the module name.</param>
/// <param name="OutputPath">The absolute or relative file path where the emitted assembly will be written. Ignored when <paramref name="OutputKind"/> is <see cref="CilOutputKind.IlSourceOnly"/>.</param>
/// <param name="OutputKind">The kind of output to produce.</param>
/// <param name="ExternalFunctions">The functions callable via <c>summon</c> or <c>summon.find</c>, or <c>null</c> for none.</param>
/// <param name="HostInjectedServices">The services available to satisfy an external function trailing host-injected parameters, or <c>null</c> for none.</param>
public sealed record CilEmitOptions(
    string AssemblyName,
    string OutputPath,
    CilOutputKind OutputKind,
    IReadOnlyList<CilExternalFunction>? ExternalFunctions = null,
    IReadOnlyList<CilHostInjectedService>? HostInjectedServices = null);
