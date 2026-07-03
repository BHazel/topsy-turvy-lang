namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Declares a variable as a named virtual register of a specified type.
/// </summary>
/// <remarks>
/// <para>
/// Corresponds to the UtopIR <c>welcome</c> instruction with the format
/// <c>£&lt;name&gt; = welcome &lt;type&gt;</c>.
/// </para>
/// <para>
/// For example, the Topsy Turvy declaration:
/// </para>
/// <code>
/// PRAY WELCOME LovesickMaidens AS A PEER
/// </code>
/// <para>
/// produces the following <see cref="WelcomeInstruction"/>:
/// </para>
/// <code>
/// new WelcomeInstruction(
///     Target: new UtopIRVariable("LovesickMaidens"),
///     Type: UtopIRType.Peer);
/// </code>
/// <para>
/// which is rendered in UtopIR source as:
/// </para>
/// <code>
/// £LovesickMaidens = welcome peer
/// </code>
/// </remarks>
/// <param name="Target">The virtual register being declared.</param>
/// <param name="Type">The type of the variable.</param>
public sealed record WelcomeInstruction(UtopIRVariable Target, UtopIRType Type) : UtopIRInstruction;
