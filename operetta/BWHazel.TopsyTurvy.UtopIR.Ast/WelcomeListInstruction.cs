namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Declares an array variable of a specified element type and size, assigning it to a virtual register.
/// </summary>
/// <remarks>
/// <para>
/// Corresponds to the UtopIR <c>welcome.list</c> instruction with the format
/// <c>£&lt;name&gt; = welcome.list &lt;type&gt;, &lt;size&gt;</c>. <see cref="Size"/> is a compile-time
/// integer literal, not an operand: no UtopIR spec form allows a variable size.
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
///     Size: 3);
/// </code>
/// <para>
/// which is rendered in UtopIR source as:
/// </para>
/// <code>
/// £Numbers = welcome.list peer, 3
/// </code>
/// </remarks>
/// <param name="Target">The virtual register being declared.</param>
/// <param name="ElementType">The type of each array element.</param>
/// <param name="Size">The number of elements in the array.</param>
public sealed record WelcomeListInstruction(UtopIRVariable Target, UtopIRType ElementType, int Size) : UtopIRInstruction;
