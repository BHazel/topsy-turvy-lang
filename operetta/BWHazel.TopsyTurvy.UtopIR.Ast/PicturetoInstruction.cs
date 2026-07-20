namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Assigns a pointer to refer to an existing variable, assigning it to a virtual register.
/// </summary>
/// <remarks>
/// <para>
/// Corresponds to the UtopIR <c>pictureto</c> instruction with the format
/// <c>£&lt;pointer-name&gt; = pictureto £&lt;variable&gt;</c>. <see cref="Pointee"/> must be an
/// already-declared variable, never a literal.
/// </para>
/// <para>
/// For example, the Topsy Turvy statement:
/// </para>
/// <code>
/// NumberPointer IS APPOINTED GALLERY PICTURE TO Number
/// </code>
/// <para>
/// produces the following <see cref="PicturetoInstruction"/>:
/// </para>
/// <code>
/// new PicturetoInstruction(
///     Target: new UtopIRVariable("NumberPointer"),
///     Pointee: new UtopIRVariable("Number"));
/// </code>
/// <para>
/// which is rendered in UtopIR source as:
/// </para>
/// <code>
/// £NumberPointer = pictureto £Number
/// </code>
/// </remarks>
/// <param name="Target">The virtual register that will hold the pointer.</param>
/// <param name="Pointee">The already-declared variable the pointer will refer to.</param>
public sealed record PicturetoInstruction(UtopIRVariable Target, UtopIRVariable Pointee) : UtopIRInstruction;
