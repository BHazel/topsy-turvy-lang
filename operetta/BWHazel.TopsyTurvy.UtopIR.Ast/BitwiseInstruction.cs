namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Applies a binary bitwise operation to two integer operands and stores the result in a virtual register.
/// </summary>
/// <remarks>
/// <para>
/// Covers the binary UtopIR bitwise mnemonics:
/// * <c>chord</c> (Bitwise AND)
/// * <c>harmony</c> (Bitwise OR)
/// * <c>discord</c> (Bitwise XOR)
/// * <c>transup</c> (Bitwise left shift by the second operand)
/// * <c>transdown</c> (Bitwise right shift by the second operand)
/// The unary bitwise NOT has its own instruction type, <see cref="InvInstruction"/>.
/// 
/// The operation to perform is selected by <see cref="Operation"/> and the format in UtopIR source is:
/// </para>
/// <code>
/// <c>£&lt;name&gt; = &lt;op&gt; &lt;op1&gt;, &lt;op2&gt;</c>.
/// </code>
/// <para>
/// For example, the Topsy Turvy expression:
/// </para>
/// <code>
/// PRAY WELCOME PeerAndResult AS A PEER BEING CHORD OF PeerMask AND PeerValue
/// </code>
/// <para>
/// results in the transformer emitting two instructions:
/// * A <see cref="BitwiseInstruction"/> to compute the AND into a temporary virtual register.
/// * Then an <see cref="AppointInstruction"/> to assign that temporary virtual register to the declared variable.
/// </para>
/// <para>
/// The <see cref="BitwiseInstruction"/> in this example would be represented in the AST as:
/// </para>
/// <code>
/// new BitwiseInstruction(
///     Operation: UtopIRBitwiseOperation.Chord,
///     Target: new UtopIRVariable("_chord_PeerMask_PeerValue"),
///     Operand1: new VariableOperand(new UtopIRVariable("PeerMask")),
///     Operand2: new VariableOperand(new UtopIRVariable("PeerValue")));
/// </code>
/// <para>
/// which is rendered in UtopIR source as:
/// </para>
/// <code>
/// £_chord_PeerMask_PeerValue = chord £PeerMask, £PeerValue
/// </code>
/// </remarks>
/// <param name="Operation">The bitwise operation to perform.</param>
/// <param name="Target">The virtual register that will hold the result.</param>
/// <param name="Operand1">The first operand.</param>
/// <param name="Operand2">The second operand; for shifts this is the shift amount.</param>
public sealed record BitwiseInstruction(
    UtopIRBitwiseOperation Operation,
    UtopIRVariable Target,
    UtopIROperand Operand1,
    UtopIROperand Operand2) : UtopIRInstruction;
