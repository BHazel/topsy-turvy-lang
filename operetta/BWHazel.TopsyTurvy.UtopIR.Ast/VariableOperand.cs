namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// An operand that references a named virtual register.
/// </summary>
/// <remarks>
/// <para>
/// For example, the UtopIR source: 
/// </para>
/// <code>
/// £Result = sum £a, £b
/// </code>
/// <para>
/// is represented in the AST as:
/// </para>
/// <code>
/// new ArithmeticInstruction(
///     Operation: ArithmeticOperator.Sum,
///     Target: new UtopIRVariable("Result"),
///     Operand1: new VariableOperand(new UtopIRVariable("a")),
///     Operand2: new VariableOperand(new UtopIRVariable("b")));
/// </code>
/// </remarks>
/// <param name="Variable">The virtual register being referenced.</param>
public sealed record VariableOperand(UtopIRVariable Variable) : UtopIROperand;
