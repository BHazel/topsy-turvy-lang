namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Applies a binary logical operation to two <c>decree</c> operands and stores the result in a virtual register.
/// </summary>
/// <remarks>
/// <para>
/// Covers the binary UtopIR logical mnemonics:
/// * <c>both</c> (Logical AND)
/// * <c>either</c> (Logical OR)
/// The unary logical NOT has its own instruction type, <see cref="HardlyInstruction"/>.
///
/// Operands must be of the <c>decree</c> type in the same instruction.  The operation to perform is
/// selected by <see cref="Operation"/> and the format in UtopIR source is:
/// </para>
/// <code>
/// <c>£&lt;name&gt; = &lt;op&gt; &lt;op1&gt;, &lt;op2&gt;</c>.
/// </code>
/// <para>
/// For example, the Topsy Turvy expression:
/// </para>
/// <code>
/// PRAY WELCOME Result AS A DECREE BEING BOTH VERITY AND NAY
/// </code>
/// <para>
/// results in the transformer emitting two instructions:
/// * A <see cref="LogicalInstruction"/> to compute the AND into a temporary virtual register.
/// * Then an <see cref="AppointInstruction"/> to assign that temporary virtual register to the declared variable.
/// </para>
/// <para>
/// The <see cref="LogicalInstruction"/> in this example would be represented in the AST as:
/// </para>
/// <code>
/// new LogicalInstruction(
///     Operation: UtopIRLogicalOperation.Both,
///     Target: new UtopIRVariable("_both_verity_nay"),
///     Operand1: new LiteralOperand(true),
///     Operand2: new LiteralOperand(false));
/// </code>
/// <para>
/// which is rendered in UtopIR source as:
/// </para>
/// <code>
/// £_both_verity_nay = both verity, nay
/// </code>
/// </remarks>
/// <param name="Operation">The logical operation to perform.</param>
/// <param name="Target">The virtual register that will hold the <c>decree</c> result.</param>
/// <param name="Operand1">The first <c>decree</c> operand.</param>
/// <param name="Operand2">The second <c>decree</c> operand.</param>
public sealed record LogicalInstruction(
    UtopIRLogicalOperation Operation,
    UtopIRVariable Target,
    UtopIROperand Operand1,
    UtopIROperand Operand2) : UtopIRInstruction;
