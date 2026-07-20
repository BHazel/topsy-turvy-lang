namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Assigns a value to the element of an array variable at a specified index.
/// </summary>
/// <remarks>
/// <para>
/// Corresponds to the UtopIR <c>appoint.victim</c> instruction with the format
/// <c>appoint.victim &lt;array&gt;, &lt;index&gt;, &lt;value&gt;</c>. <see cref="Array"/> must already be
/// declared, <see cref="Index"/> must be of an integer type and <see cref="Value"/> must have a type
/// compatible with the array declared element type; using any other types is a compilation error.
/// </para>
/// <para>
/// For example, the Topsy Turvy statement:
/// </para>
/// <code>
/// VICTIM 1 ON Numbers IS APPOINTED 10
/// </code>
/// <para>
/// produces the following <see cref="AppointVictimInstruction"/>:
/// </para>
/// <code>
/// new AppointVictimInstruction(
///     Array: new UtopIRVariable("Numbers"),
///     Index: new LiteralOperand(1),
///     Value: new LiteralOperand(10));
/// </code>
/// <para>
/// which is rendered in UtopIR source as:
/// </para>
/// <code>
/// appoint.victim £Numbers, 1, 10
/// </code>
/// </remarks>
/// <param name="Array">The array variable whose element is being assigned.</param>
/// <param name="Index">The 1-based integer index of the element being assigned, either a literal or variable.</param>
/// <param name="Value">The value to assign to the array element, either a literal or variable.</param>
public sealed record AppointVictimInstruction(UtopIRVariable Array, UtopIROperand Index, UtopIROperand Value) : UtopIRInstruction;
