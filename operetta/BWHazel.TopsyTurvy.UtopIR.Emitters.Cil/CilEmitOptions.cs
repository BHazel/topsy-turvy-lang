namespace BWHazel.TopsyTurvy.UtopIR.Emitters.Cil;

/// <summary>
/// Options controlling how the <see cref="CilEmitter"/> produces a .NET assembly.
/// </summary>
/// <param name="AssemblyName">The name of the assembly used as the CLR assembly identity and the module name.</param>
/// <param name="OutputPath">The absolute or relative file path where the emitted assembly will be written. Ignored when <paramref name="OutputKind"/> is <see cref="CilOutputKind.IlSourceOnly"/>.</param>
/// <param name="OutputKind">The kind of output to produce.</param>
public sealed record CilEmitOptions(
    string AssemblyName,
    string OutputPath,
    CilOutputKind OutputKind);
