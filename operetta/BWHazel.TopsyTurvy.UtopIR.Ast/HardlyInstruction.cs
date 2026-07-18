namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Applies the logical NOT operation to a single <c>decree</c> operand and stores the result in a virtual register.
/// </summary>
/// <remarks>
/// <para>
/// This is the only unary logical instruction; the binary logical operations are covered by
/// <see cref="LogicalInstruction"/>.  Corresponds to the Topsy Turvy <c>HARDLY EVER x</c> operator
/// and the format in UtopIR source is:
/// </para>
/// <code>
/// <c>£&lt;name&gt; = hardly &lt;op1&gt;</c>.
/// </code>
/// <para>
/// For example, the Topsy Turvy expression:
/// </para>
/// <code>
/// PRAY WELCOME Result AS A DECREE BEING HARDLY EVER VERITY
/// </code>
/// <para>
/// results in the transformer emitting a <see cref="HardlyInstruction"/> to compute the NOT into a
/// temporary virtual register.  The <see cref="HardlyInstruction"/> in this example would be
/// represented in the AST as:
/// </para>
/// <code>
/// new HardlyInstruction(
///     Target: new UtopIRVariable("_hardly_verity"),
///     Operand: new LiteralOperand(true));
/// </code>
/// <para>
/// which is rendered in UtopIR source as:
/// </para>
/// <code>
/// £_hardly_verity = hardly verity
/// </code>
/// </remarks>
/// <param name="Target">The virtual register that will hold the <c>decree</c> result.</param>
/// <param name="Operand">The <c>decree</c> operand to negate.</param>
public sealed record HardlyInstruction(
    UtopIRVariable Target,
    UtopIROperand Operand) : UtopIRInstruction;
