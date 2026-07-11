namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Defines constants for the binary arithmetic operations in the UtopIR instruction set.
/// </summary>
/// <remarks>
/// <para>
/// Each value corresponds to a UtopIR arithmetic instruction mnemonic and a Topsy Turvy arithmetic
/// operator.  All operations are binary: they consume exactly two operands and produce one result.
/// Integer and floating-point arithmetic are separate instruction groups: the floating-point
/// instructions have the same names as their integer counterparts with a <c>.f</c> suffix appended.
/// </para>
/// <para>
/// For each UtopIR integer arithmetic operation, there is a corresponding Topsy Turvy operator:
/// * <see cref="UtopIRArithmeticOperation"/><c>.Sum</c>: <c>sum</c> (<c>SUM OF x AND y</c>)
/// * <see cref="UtopIRArithmeticOperation"/><c>.Diff</c>: <c>diff</c> (<c>DIFFERENCE OF x AND y</c>)
/// * <see cref="UtopIRArithmeticOperation"/><c>.Prod</c>: <c>prod</c> (<c>PRODUCT OF x AND y</c>)
/// * <see cref="UtopIRArithmeticOperation"/><c>.Quot</c>: <c>quot</c> (<c>QUOTIENT OF x AND y</c>)
/// * <see cref="UtopIRArithmeticOperation"/><c>.Rem</c>: <c>rem</c> (<c>REMAINDER OF x AND y</c>)
/// * <see cref="UtopIRArithmeticOperation"/><c>.Max</c>: <c>max</c> (<c>LARGER OF x AND y</c>)
/// * <see cref="UtopIRArithmeticOperation"/><c>.Min</c>: <c>min</c> (<c>SMALLER OF x AND y</c>)
/// </para>
/// <para>
/// Topsy Turvy has no separate syntax for floating-point arithmetic: <c>SUM OF x AND y</c> is
/// written identically whether <c>x</c>/<c>y</c> are integers or floats.  The floating-point
/// operations below map to the same Topsy Turvy operators as their integer counterparts.  The
/// transformer chooses which group to emit purely from the operand types it has already resolved,
/// not from anything distinguishable in the source text itself:
/// * <see cref="UtopIRArithmeticOperation"/><c>.SumFloat</c>: <c>sum.f</c>
/// * <see cref="UtopIRArithmeticOperation"/><c>.DiffFloat</c>: <c>diff.f</c>
/// * <see cref="UtopIRArithmeticOperation"/><c>.ProdFloat</c>: <c>prod.f</c>
/// * <see cref="UtopIRArithmeticOperation"/><c>.QuotFloat</c>: <c>quot.f</c>
/// * <see cref="UtopIRArithmeticOperation"/><c>.RemFloat</c>: <c>rem.f</c>
/// * <see cref="UtopIRArithmeticOperation"/><c>.MaxFloat</c>: <c>max.f</c>
/// * <see cref="UtopIRArithmeticOperation"/><c>.MinFloat</c>: <c>min.f</c>
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

    /// <summary>sum.f: SUM OF x AND y on floating-point operands</summary>
    SumFloat,

    /// <summary>diff.f: DIFFERENCE OF x AND y on floating-point operands</summary>
    DiffFloat,

    /// <summary>prod.f: PRODUCT OF x AND y on floating-point operands</summary>
    ProdFloat,

    /// <summary>quot.f: QUOTIENT OF x AND y on floating-point operands</summary>
    QuotFloat,

    /// <summary>rem.f: REMAINDER OF x AND y on floating-point operands</summary>
    RemFloat,

    /// <summary>max.f: LARGER OF x AND y on floating-point operands</summary>
    MaxFloat,

    /// <summary>min.f: SMALLER OF x AND y on floating-point operands</summary>
    MinFloat,
}
