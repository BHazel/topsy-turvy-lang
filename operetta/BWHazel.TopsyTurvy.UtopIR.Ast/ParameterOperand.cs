namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// An operand that references a function parameter.
/// </summary>
/// <remarks>
/// <para>
/// For example, the UtopIR source:
/// </para>
/// <code>
/// £Result = sum %Num1, %Num2
/// </code>
/// <para>
/// is represented in the AST as:
/// </para>
/// <code>
/// new ArithmeticInstruction(
///     Operation: UtopIRArithmeticOperation.Sum,
///     Target: new UtopIRVariable("Result"),
///     Operand1: new ParameterOperand(new UtopIRParameter("Num1")),
///     Operand2: new ParameterOperand(new UtopIRParameter("Num2")));
/// </code>
/// </remarks>
/// <param name="Parameter">The function parameter being referenced.</param>
public sealed record ParameterOperand(UtopIRParameter Parameter) : UtopIROperand;
