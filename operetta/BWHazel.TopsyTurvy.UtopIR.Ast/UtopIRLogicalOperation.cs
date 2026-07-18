namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Defines constants for the binary logical operations in the UtopIR instruction set.
/// </summary>
/// <remarks>
/// <para>
/// Each value corresponds to a UtopIR logical instruction mnemonic and a Topsy Turvy logical
/// operator.  All operations here are binary: they consume exactly two <c>decree</c> operands and
/// produce one <c>decree</c> result.  The unary logical NOT is represented separately by
/// <see cref="HardlyInstruction"/> and is therefore not included in this enumeration.
/// </para>
/// <para>
/// For each UtopIR logical operation, there is a corresponding Topsy Turvy operator:
/// * <see cref="UtopIRLogicalOperation"/><c>.Both</c>: <c>both</c> (<c>BOTH x AND y</c>)
/// * <see cref="UtopIRLogicalOperation"/><c>.Either</c>: <c>either</c> (<c>EITHER x OR y</c>)
/// </para>
/// </remarks>
public enum UtopIRLogicalOperation
{
    /// <summary>both: Logical AND, BOTH x AND y</summary>
    Both,

    /// <summary>either: Logical OR, EITHER x OR y</summary>
    Either,
}
