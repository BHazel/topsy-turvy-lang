namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Declares a pointer variable of a specified pointee type, assigning it to a virtual register.
/// </summary>
/// <remarks>
/// <para>
/// Corresponds to the UtopIR <c>welcome.gallerypic</c> instruction with the format
/// <c>£&lt;name&gt; = welcome.gallerypic &lt;type&gt;</c>.
/// </para>
/// <para>
/// For example, the Topsy Turvy declaration:
/// </para>
/// <code>
/// PRAY WELCOME NumberPointer AS A GALLERY PICTURE OF PEER
/// </code>
/// <para>
/// produces the following <see cref="WelcomeGallerypicInstruction"/>:
/// </para>
/// <code>
/// new WelcomeGallerypicInstruction(
///     Target: new UtopIRVariable("NumberPointer"),
///     PointeeType: UtopIRType.Peer);
/// </code>
/// <para>
/// which is rendered in UtopIR source as:
/// </para>
/// <code>
/// £NumberPointer = welcome.gallerypic peer
/// </code>
/// </remarks>
/// <param name="Target">The virtual register being declared.</param>
/// <param name="PointeeType">The type of the value this pointer refers to.</param>
public sealed record WelcomeGallerypicInstruction(UtopIRVariable Target, UtopIRType PointeeType) : UtopIRInstruction;
