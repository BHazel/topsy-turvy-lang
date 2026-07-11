namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Applies the bitwise NOT operation to a single integer operand and stores the result in a virtual register.
/// </summary>
/// <remarks>
/// <para>
/// This is only unary bitwise instruction; the binary bitwise operations are covered by
/// <see cref="BitwiseInstruction"/>.  Corresponds to the Topsy Turvy <c>INVERSION OF x</c>
/// operator and the format in UtopIR source is:
/// </para>
/// <code>
/// <c>£&lt;name&gt; = inv &lt;op1&gt;</c>.
/// </code>
/// <para>
/// For example, the Topsy Turvy expression:
/// </para>
/// <code>
/// PRAY WELCOME PeerNotResult AS A PEER BEING INVERSION OF PeerAndResult
/// </code>
/// <para>
/// results in the transformer emitting an <see cref="InvInstruction"/> to compute the NOT into a
/// temporary virtual register.  The <see cref="InvInstruction"/> in this example would be
/// represented in the AST as:
/// </para>
/// <code>
/// new InvInstruction(
///     Target: new UtopIRVariable("_inv_PeerAndResult"),
///     Operand: new VariableOperand(new UtopIRVariable("PeerAndResult")));
/// </code>
/// <para>
/// which is rendered in UtopIR source as:
/// </para>
/// <code>
/// £_inv_PeerAndResult = inv £PeerAndResult
/// </code>
/// </remarks>
/// <param name="Target">The virtual register that will hold the result.</param>
/// <param name="Operand">The integer operand to invert.</param>
public sealed record InvInstruction(
    UtopIRVariable Target,
    UtopIROperand Operand) : UtopIRInstruction;
