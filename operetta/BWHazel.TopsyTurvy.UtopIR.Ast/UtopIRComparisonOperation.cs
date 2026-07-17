namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Defines constants for the binary comparison operations in the UtopIR instruction set.
/// </summary>
/// <remarks>
/// <para>
/// Each value corresponds to a UtopIR comparison instruction mnemonic and a Topsy Turvy comparison
/// operator.  All operations are binary: they consume exactly two operands of the same type and
/// produce one <c>decree</c> (boolean) result, regardless of the operand type.  Integer and
/// floating-point comparison are separate instruction groups: the floating-point instructions have
/// the same names as their integer counterparts with a <c>.f</c> suffix appended.
/// </para>
/// <para>
/// For each UtopIR integer comparison operation, there is a corresponding Topsy Turvy operator:
/// * <see cref="UtopIRComparisonOperation"/><c>.Alike</c>: <c>alike</c> (<c>ALIKE x AND y</c>)
/// * <see cref="UtopIRComparisonOperation"/><c>.Unlike</c>: <c>unlike</c> (<c>UNLIKE x AND y</c>)
/// * <see cref="UtopIRComparisonOperation"/><c>.PreAdam</c>: <c>preadam</c> (<c>PRE-ADAMITE x AND y</c>)
/// * <see cref="UtopIRComparisonOperation"/><c>.LowerDeg</c>: <c>lowerdeg</c> (<c>LOWER DEGREE x AND y</c>)
/// </para>
/// <para>
/// Topsy Turvy has no separate syntax for floating-point comparison: <c>ALIKE x AND y</c> is written
/// identically whether <c>x</c>/<c>y</c> are integers or floats.  The floating-point operations below
/// map to the same Topsy Turvy operators as their integer counterparts.  The transformer chooses
/// which group to emit purely from the operand types it has already resolved, not from anything
/// distinguishable in the source text itself:
/// * <see cref="UtopIRComparisonOperation"/><c>.AlikeFloat</c>: <c>alike.f</c>
/// * <see cref="UtopIRComparisonOperation"/><c>.UnlikeFloat</c>: <c>unlike.f</c>
/// * <see cref="UtopIRComparisonOperation"/><c>.PreAdamFloat</c>: <c>preadam.f</c>
/// * <see cref="UtopIRComparisonOperation"/><c>.LowerDegFloat</c>: <c>lowerdeg.f</c>
/// </para>
/// </remarks>
public enum UtopIRComparisonOperation
{
    /// <summary>alike: Equality, ALIKE x AND y</summary>
    Alike,

    /// <summary>unlike: Inequality, UNLIKE x AND y</summary>
    Unlike,

    /// <summary>preadam: Greater-Than, PRE-ADAMITE x AND y</summary>
    PreAdam,

    /// <summary>lowerdeg: Less-Than, LOWER DEGREE x AND y</summary>
    LowerDeg,

    /// <summary>alike.f: Equality on floating-point operands, ALIKE x AND y</summary>
    AlikeFloat,

    /// <summary>unlike.f: Inequality on floating-point operands, UNLIKE x AND y</summary>
    UnlikeFloat,

    /// <summary>preadam.f: Greater-Than on floating-point operands, PRE-ADAMITE x AND y</summary>
    PreAdamFloat,

    /// <summary>lowerdeg.f: Less-Than on floating-point operands, LOWER DEGREE x AND y</summary>
    LowerDegFloat,
}
