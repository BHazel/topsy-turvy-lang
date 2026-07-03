namespace BWHazel.TopsyTurvy.UtopIR.Emitters.Cil;

/// <summary>
/// The result from emitting a UtopIR program to CIL bytecode.
/// </summary>
/// <param name="IlSource">A human-readable IL listing of the emitted programme.</param>
/// <remarks>
/// In the human-readable CIL, locals are shown using their original UtopIR variable names,
/// rather than raw numeric slots.  This is not the output of a disassembler such as
/// <c>ildasm</c> but is a direct textual record of what the emitter itself generated,
/// so it is always in sync with the bytecode but omits low-level detail (byte offsets,
/// exact metadata tokens) that a true disassembler would show.
/// </remarks>
public sealed record CilEmitResult(string IlSource);
