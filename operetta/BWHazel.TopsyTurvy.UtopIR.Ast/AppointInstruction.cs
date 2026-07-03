namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Assigns a value to a virtual register.
/// </summary>
/// <remarks>
/// <para>
/// Corresponds to the UtopIR <c>appoint</c> instruction with the format
/// <c>£&lt;name&gt; = appoint &lt;value&gt;</c>.  The <see cref="Value"/> operand may be a
/// compile-time constant (<see cref="LiteralOperand"/>) or the current value of another
/// virtual register (<see cref="VariableOperand"/>).
/// </para>
/// <para>
/// For example, the Topsy Turvy assignment:
/// </para>
/// <code>
/// LovesickMaidens IS APPOINTED 20
/// </code>
/// <para>
/// produces the following <see cref="AppointInstruction"/>:
/// </para>
/// <code>
/// new AppointInstruction(
///     Target: new UtopIRVariable("LovesickMaidens"),
///     Value: new LiteralOperand(20));
/// </code>
/// <para>
/// which is rendered in UtopIR source as:
/// </para>
/// <code>
/// £LovesickMaidens = appoint 20
/// </code>
/// </remarks>
/// <param name="Target">The virtual register receiving the assigned value.</param>
/// <param name="Value">The operand supplying the value; either a literal or a variable reference.</param>
public sealed record AppointInstruction(UtopIRVariable Target, UtopIROperand Value) : UtopIRInstruction;
