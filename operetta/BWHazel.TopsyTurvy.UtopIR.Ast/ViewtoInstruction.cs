namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Assigns a value through a pointer, replacing the value it refers to.
/// </summary>
/// <remarks>
/// <para>
/// Corresponds to the UtopIR <c>viewto</c> instruction with the format
/// <c>viewto £&lt;pointer&gt;, &lt;value&gt;</c>. <see cref="Pointer"/> must be an already-declared and
/// assigned pointer, and <see cref="Value"/> must have the same type as the pointee type of the
/// pointer; using any other type is a compilation error.
/// </para>
/// <para>
/// For example, the Topsy Turvy statement:
/// </para>
/// <code>
/// VIEW FROM NumberPointer IS APPOINTED 23
/// </code>
/// <para>
/// produces the following <see cref="ViewtoInstruction"/>:
/// </para>
/// <code>
/// new ViewtoInstruction(
///     Pointer: new UtopIRVariable("NumberPointer"),
///     Value: new LiteralOperand(23));
/// </code>
/// <para>
/// which is rendered in UtopIR source as:
/// </para>
/// <code>
/// viewto £NumberPointer, 23
/// </code>
/// </remarks>
/// <param name="Pointer">The pointer used to assign the value.</param>
/// <param name="Value">The value to assign, either a literal or variable.</param>
public sealed record ViewtoInstruction(UtopIRVariable Pointer, UtopIROperand Value) : UtopIRInstruction;
