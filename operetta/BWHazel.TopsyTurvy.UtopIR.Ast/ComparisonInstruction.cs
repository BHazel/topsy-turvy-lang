namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Applies a binary comparison operation to two operands and stores the result as a <c>decree</c> in a virtual register.
/// </summary>
/// <remarks>
/// <para>
/// Covers all UtopIR comparison mnemonics:
/// * <c>alike</c> / <c>alike.f</c>
/// * <c>unlike</c> / <c>unlike.f</c>
/// * <c>preadam</c> / <c>preadam.f</c>
/// * <c>lowerdeg</c> / <c>lowerdeg.f</c>
/// The plain mnemonics operate on integer, or <c>stitch</c>, operands and the <c>.f</c>-suffixed
/// mnemonics on floating-point operands.  The result type is always
/// <c>decree</c> regardless of the operand type.  The operation to perform is
/// selected by <see cref="Operation"/> and the format in UtopIR source is:
/// </para>
/// <code>
/// <c>£&lt;name&gt; = &lt;op&gt; &lt;op1&gt;, &lt;op2&gt;</c>.
/// </code>
/// <para>
/// For example, the Topsy Turvy expression used as an initial value:
/// </para>
/// <code>
/// PRAY WELCOME Equal AS A DECREE BEING ALIKE NumLords AND NumMaidens
/// </code>
/// <para>
/// results in the transformer emitting two instructions:
/// * A <see cref="ComparisonInstruction"/> to compute the equality into a temporary virtual register.
/// * Then an <see cref="AppointInstruction"/> to assign that temporary virtual register to the declared variable.
/// </para>
/// <para>
/// The <see cref="ComparisonInstruction"/> in this example would be represented in the AST as:
/// </para>
/// <code>
/// new ComparisonInstruction(
///     Operation: UtopIRComparisonOperation.Alike,
///     Target: new UtopIRVariable("_alike_NumLords_NumMaidens"),
///     Operand1: new VariableOperand(new UtopIRVariable("NumLords")),
///     Operand2: new VariableOperand(new UtopIRVariable("NumMaidens")));
/// </code>
/// <para>
/// which is rendered in UtopIR source as:
/// </para>
/// <code>
/// £_alike_NumLords_NumMaidens = alike £NumLords, £NumMaidens
/// </code>
/// </remarks>
/// <param name="Operation">The comparison operation to perform.</param>
/// <param name="Target">The virtual register that will hold the <c>decree</c> result.</param>
/// <param name="Operand1">The first operand.</param>
/// <param name="Operand2">The second operand.</param>
public sealed record ComparisonInstruction(
    UtopIRComparisonOperation Operation,
    UtopIRVariable Target,
    UtopIROperand Operand1,
    UtopIROperand Operand2) : UtopIRInstruction;
