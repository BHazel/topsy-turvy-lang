namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Applies a binary arithmetic operation to two operands and stores the result in a virtual register.
/// </summary>
/// <remarks>
/// <para>
/// Covers all UtopIR arithmetic mnemonics:
/// * <c>sum</c> / <c>sum.f</c>
/// * <c>diff</c> / <c>diff.f</c>
/// * <c>prod</c> / <c>prod.f</c>
/// * <c>quot</c> / <c>quot.f</c>
/// * <c>rem</c> / <c>rem.f</c>
/// * <c>max</c> / <c>max.f</c>
/// * <c>min</c> / <c>min.f</c>
/// The plain mnemonics operate on integer operands and the <c>.f</c>-suffixed mnemonics on
/// floating-point operands.  The operation to perform is selected by <see cref="Operation"/> and the
/// format in UtopIR source is:
/// </para>
/// <code>
/// <c>£&lt;name&gt; = &lt;op&gt; &lt;op1&gt;, &lt;op2&gt;</c>.
/// </code>
/// <para>
/// For example, the Topsy Turvy expression used as an initial value:
/// </para>
/// <code>
/// PRAY WELCOME PeerResultA AS A PEER BEING SUM OF Peer1 AND Peer2
/// </code>
/// <para>
/// results in the transformer emitting two instructions:
/// * An <see cref="ArithmeticInstruction"/> to compute the sum into a temporary virtual register.
/// * Then an <see cref="AppointInstruction"/> to assign that temporary virtual register to the declared variable,
/// </para>
/// <para>
/// The <see cref="ArithmeticInstruction"/> in this example would be represented in the AST as:
/// </para>
/// <code>
/// new ArithmeticInstruction(
///     Op: UtopIRArithmeticOp.Sum,
///     Target: new UtopIRVariable("_sum_Peer1_Peer2"),
///     Operand1: new VariableOperand(new UtopIRVariable("Peer1")),
///     Operand2: new VariableOperand(new UtopIRVariable("Peer2")));
/// </code>
/// <para>
/// which is rendered in UtopIR source as:
/// </para>
/// <code>
/// £_sum_Peer1_Peer2 = sum £Peer1, £Peer2
/// </code>
/// </remarks>
/// <param name="Operation">The arithmetic operation to perform.</param>
/// <param name="Target">The virtual register that will hold the result.</param>
/// <param name="Operand1">The first operand.</param>
/// <param name="Operand2">The second operand.</param>
public sealed record ArithmeticInstruction(
    UtopIRArithmeticOperation Operation,
    UtopIRVariable Target,
    UtopIROperand Operand1,
    UtopIROperand Operand2) : UtopIRInstruction;
