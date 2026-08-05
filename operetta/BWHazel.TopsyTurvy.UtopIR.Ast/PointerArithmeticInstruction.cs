namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Applies additive or subtractive pointer arithmetic to a pointer by an integer offset, assigning
/// the result to a virtual register.
/// </summary>
/// <remarks>
/// <para>
/// Corresponds to the UtopIR <c>sum.g</c> and <c>diff.g</c> instructions with the format
/// <c>£&lt;name&gt; = &lt;op&gt; &lt;pointer&gt;, &lt;offset&gt;</c>. <see cref="Pointer"/> must be an
/// already-declared and assigned pointer, and <see cref="Offset"/> must be of an integer type; using
/// any other types is a compilation error.
/// </para>
/// <para>
/// For example, the Topsy Turvy expression:
/// </para>
/// <code>
/// NumbersPointerOffset1 IS APPOINTED SUM OF NumbersPointer AND 1
/// </code>
/// <para>
/// produces the following <see cref="PointerArithmeticInstruction"/>:
/// </para>
/// <code>
/// new PointerArithmeticInstruction(
///     Operation: UtopIRPointerArithmeticOperation.Sum,
///     Target: new UtopIRVariable("NumbersPointerOffset1"),
///     Pointer: new UtopIRVariable("NumbersPointer"),
///     Offset: new LiteralOperand(1));
/// </code>
/// <para>
/// which is rendered in UtopIR source as:
/// </para>
/// <code>
/// £NumbersPointerOffset1 = sum.g £NumbersPointer, 1
/// </code>
/// </remarks>
/// <param name="Operation">The pointer arithmetic operation to perform.</param>
/// <param name="Target">The virtual register that will hold the resulting pointer.</param>
/// <param name="Pointer">The pointer being adjusted.</param>
/// <param name="Offset">The integer offset to adjust the pointer by, either a literal or variable.</param>
public sealed record PointerArithmeticInstruction(
    UtopIRPointerArithmeticOperation Operation,
    UtopIRVariable Target,
    UtopIRVariable Pointer,
    UtopIROperand Offset) : UtopIRInstruction;
