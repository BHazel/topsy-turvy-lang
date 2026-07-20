namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Defines constants for the pointer arithmetic operations in the UtopIR instruction set.
/// </summary>
/// <remarks>
/// Kept separate from <see cref="UtopIRArithmeticOperation"/> since a pointer arithmetic operand
/// pair (pointer, integer offset) has a different shape to same-type arithmetic operands, even
/// though both lower from the same Topsy Turvy <c>SUM OF</c>/<c>DIFFERENCE OF</c> operators:
/// * <see cref="UtopIRPointerArithmeticOperation"/><c>.Sum</c>: <c>sum.g</c> (<c>SUM OF pointer AND offset</c>)
/// * <see cref="UtopIRPointerArithmeticOperation"/><c>.Diff</c>: <c>diff.g</c> (<c>DIFFERENCE OF pointer AND offset</c>)
/// </remarks>
public enum UtopIRPointerArithmeticOperation
{
    /// <summary>sum.g: SUM OF pointer AND offset</summary>
    Sum,

    /// <summary>diff.g: DIFFERENCE OF pointer AND offset</summary>
    Diff,
}
