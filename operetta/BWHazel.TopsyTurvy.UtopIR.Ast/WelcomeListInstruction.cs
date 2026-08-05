namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Declares an array variable of a specified element type and size, assigning it to a virtual register.
/// </summary>
/// <remarks>
/// <para>
/// Corresponds to the UtopIR <c>welcome.list</c> instruction with the format
/// <c>£&lt;name&gt; = welcome.list &lt;type&gt;, &lt;size&gt;</c>. <see cref="Size"/> is an <see cref="UtopIROperand"/>,
/// either a compile-time literal or a variable already holding the desired size.
/// </para>
/// <para>
/// For example, the Topsy Turvy declaration:
/// </para>
/// <code>
/// PRAY WELCOME Numbers AS A LITTLE LIST OF 3 PEER
/// </code>
/// <para>
/// produces the following <see cref="WelcomeListInstruction"/>:
/// </para>
/// <code>
/// new WelcomeListInstruction(
///     Target: new UtopIRVariable("Numbers"),
///     ElementType: UtopIRType.Peer,
///     Size: new LiteralOperand(3));
/// </code>
/// <para>
/// which is rendered in UtopIR source as:
/// </para>
/// <code>
/// £Numbers = welcome.list peer, 3
/// </code>
/// <para>
/// A variable size, from a Topsy Turvy declaration such as <c>PRAY WELCOME Numbers AS A LITTLE LIST OF n PEER</c>,
/// produces a <see cref="VariableOperand"/> instead, rendered as <c>£Numbers = welcome.list peer, £n</c>.
/// </para>
/// </remarks>
/// <param name="Target">The virtual register being declared.</param>
/// <param name="ElementType">The type of each array element.</param>
/// <param name="Size">The number of elements in the array.</param>
public sealed record WelcomeListInstruction(UtopIRVariable Target, UtopIRType ElementType, UtopIROperand Size) : UtopIRInstruction;
