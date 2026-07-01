namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Base class for all instructions in the UtopIR instruction set.
/// </summary>
/// <remarks>
/// Every UtopIR instruction is an element of a <see cref="UtopIRProgram"/>.  Concrete subtypes represent
/// individual operations.
/// </remarks>
public abstract record UtopIRInstruction;
