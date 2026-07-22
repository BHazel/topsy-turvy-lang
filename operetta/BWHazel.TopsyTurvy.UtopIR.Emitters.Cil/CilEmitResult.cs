namespace BWHazel.TopsyTurvy.UtopIR.Emitters.Cil;

/// <summary>
/// The result from emitting a UtopIR program to CIL bytecode.
/// </summary>
/// <param name="IlSource">A human-readable IL listing of the emitted programme.</param>
/// <param name="HasExternalCalls">A value indicating whether the programme emitted at least one <c>summon</c> or <c>summon.find</c> call.</param>
/// <remarks>
/// <para>
/// In the human-readable CIL, locals are shown using their original UtopIR variable names,
/// rather than raw numeric slots.  This is not the output of a disassembler such as
/// <c>ildasm</c> but is a direct textual record of what the emitter itself generated,
/// so it is always in sync with the bytecode but omits low-level detail (byte offsets,
/// exact metadata tokens) that a true disassembler would show.
/// </para>
/// <para>
/// <paramref name="HasExternalCalls"/> lets a caller decide whether the emitted assembly dependencies
/// need to be deployed alongside it, without having to re-scan <see cref="IlSource"/> or the original programme.
/// </para>
/// </remarks>
public sealed record CilEmitResult(string IlSource, bool HasExternalCalls = false);
