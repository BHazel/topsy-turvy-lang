namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Base class for a value that can appear as an operand in a UtopIR instruction.
/// </summary>
/// <remarks>
/// An operand is either a:
/// * Literal constant (see <see cref="LiteralOperand"/>).
/// * Reference to a named virtual register (see <see cref="VariableOperand"/>).
/// * Reference to a function parameter (see <see cref="ParameterOperand"/>).
/// </remarks>
public abstract record UtopIROperand;
