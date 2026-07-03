namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Defines constants for the binary arithmetic operations in the UtopIR instruction set.
/// </summary>
/// <remarks>
/// <para>
/// Each value corresponds to a UtopIR arithmetic instruction mnemonic and a Topsy Turvy arithmetic
/// operator.  All operations are binary: they consume exactly two operands and produce one result.
/// </para>
/// <para>
/// For each UtopIR arithmetic operation, there is a corresponding Topsy Turvy operator:
/// * <see cref="UtopIRArithmeticOperation"/><c>.Sum</c>: <c>sum</c> (<c>SUM OF x AND y</c>)
/// * <see cref="UtopIRArithmeticOperation"/><c>.Diff</c>: <c>diff</c> (<c>DIFFERENCE OF x AND y</c>)
/// * <see cref="UtopIRArithmeticOperation"/><c>.Prod</c>: <c>prod</c> (<c>PRODUCT OF x AND y</c>)
/// * <see cref="UtopIRArithmeticOperation"/><c>.Quot</c>: <c>quot</c> (<c>QUOTIENT OF x AND y</c>)
/// * <see cref="UtopIRArithmeticOperation"/><c>.Rem</c>: <c>rem</c> (<c>REMAINDER OF x AND y</c>)
/// * <see cref="UtopIRArithmeticOperation"/><c>.Max</c>: <c>max</c> (<c>LARGER OF x AND y</c>)
/// * <see cref="UtopIRArithmeticOperation"/><c>.Min</c>: <c>min</c> (<c>SMALLER OF x AND y</c>)
/// </para>
/// </remarks>
public enum UtopIRArithmeticOperation
{
    /// <summary>sum: SUM OF x AND y</summary>
    Sum,

    /// <summary>diff: DIFFERENCE OF x AND y</summary>
    Diff,

    /// <summary>prod: PRODUCT OF x AND y</summary>
    Prod,

    /// <summary>quot: QUOTIENT OF x AND y</summary>
    Quot,

    /// <summary>rem: REMAINDER OF x AND y</summary>
    Rem,

    /// <summary>max: LARGER OF x AND y</summary>
    Max,

    /// <summary>min: SMALLER OF x AND y</summary>
    Min,
}
