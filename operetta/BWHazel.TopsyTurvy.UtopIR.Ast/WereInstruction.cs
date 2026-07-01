namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Casts a value to a different type and assigns it to a virtual register.
/// </summary>
/// <remarks>
/// <para>
/// Corresponds to the UtopIR <c>were</c> instruction with the format
/// <c>£&lt;name&gt; = were &lt;value&gt;, &lt;type&gt;</c>.  The <see cref="Value"/> operand may be a
/// compile-time constant (<see cref="LiteralOperand"/>) or the current value of another
/// virtual register (<see cref="VariableOperand"/>).  <see cref="Type"/> is the destination type.
/// </para>
/// <para>
/// For example, the Topsy Turvy assignment:
/// </para>
/// <code>
/// LovesickMaidens IS APPOINTED AS IT WERE Lords AS A CHANCELLOR
/// </code>
/// <para>
/// produces the following <see cref="WereInstruction"/>:
/// </para>
/// <code>
/// new WereInstruction(
///     Target: new UtopIRVariable("LovesickMaidens"),
///     Value: new VariableOperand(new UtopIRVariable("Lords")),
///     Type: UtopIRType.Chancellor);
/// </code>
/// <para>
/// which is rendered in UtopIR source as:
/// </para>
/// <code>
/// £LovesickMaidens = were £Lords, chancellor
/// </code>
/// </remarks>
/// <param name="Target">The virtual register receiving the cast value.</param>
/// <param name="Value">The operand supplying the value to cast; either a literal or a variable reference.</param>
/// <param name="Type">The destination type of the cast.</param>
public sealed record WereInstruction(UtopIRVariable Target, UtopIROperand Value, UtopIRType Type) : UtopIRInstruction;
