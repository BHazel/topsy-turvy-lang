namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Dereferences a pointer and assigns the value it refers to, to a virtual register.
/// </summary>
/// <remarks>
/// <para>
/// Corresponds to the UtopIR <c>viewfrom</c> instruction with the format
/// <c>£&lt;name&gt; = viewfrom £&lt;pointer&gt;</c>. <see cref="Pointer"/> must be an already-declared
/// and assigned pointer, and <see cref="Target"/> must have the same type as the pointee type of the
/// pointer; using any other type is a compilation error.
/// </para>
/// <para>
/// For example, the Topsy Turvy declaration:
/// </para>
/// <code>
/// NumberValue IS APPOINTED VIEW FROM NumberPointer
/// </code>
/// <para>
/// produces the following <see cref="ViewfromInstruction"/>:
/// </para>
/// <code>
/// new ViewfromInstruction(
///     Target: new UtopIRVariable("NumberValue"),
///     Pointer: new UtopIRVariable("NumberPointer"));
/// </code>
/// <para>
/// which is rendered in UtopIR source as:
/// </para>
/// <code>
/// £NumberValue = viewfrom £NumberPointer
/// </code>
/// </remarks>
/// <param name="Target">The virtual register that will hold the dereferenced value.</param>
/// <param name="Pointer">The pointer being dereferenced.</param>
public sealed record ViewfromInstruction(UtopIRVariable Target, UtopIRVariable Pointer) : UtopIRInstruction;
