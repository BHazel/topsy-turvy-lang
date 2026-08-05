namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Selects the element at a 1-based index of an array variable and assigns it to a virtual register.
/// </summary>
/// <remarks>
/// <para>
/// Corresponds to the UtopIR <c>victim.list</c> instruction with the format
/// <c>£&lt;name&gt; = victim.list &lt;array&gt;, &lt;index&gt;</c>. <see cref="Array"/> must already be
/// declared, <see cref="Target"/> must have the same type as the declared element type of the array and
/// <see cref="Index"/> must be of an integer type; using any other types is a compilation error.
/// </para>
/// <para>
/// For example, the Topsy Turvy declaration:
/// </para>
/// <code>
/// NumbersElement2 IS APPOINTED VICTIM 2 ON Numbers
/// </code>
/// <para>
/// produces the following <see cref="VictimListInstruction"/>:
/// </para>
/// <code>
/// new VictimListInstruction(
///     Target: new UtopIRVariable("NumbersElement2"),
///     Array: new UtopIRVariable("Numbers"),
///     Index: new LiteralOperand(2));
/// </code>
/// <para>
/// which is rendered in UtopIR source as:
/// </para>
/// <code>
/// £NumbersElement2 = victim.list £Numbers, 2
/// </code>
/// </remarks>
/// <param name="Target">The virtual register that will hold the selected array element.</param>
/// <param name="Array">The array variable being selected from; must be an already declared and assigned variable.</param>
/// <param name="Index">The 1-based integer index of the element in the array, either a literal or variable.</param>
public sealed record VictimListInstruction(UtopIRVariable Target, UtopIRVariable Array, UtopIROperand Index) : UtopIRInstruction;
