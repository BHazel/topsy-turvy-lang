namespace BWHazel.TopsyTurvy.UtopIR.Ast;

/// <summary>
/// Defines constants for the binary bitwise operations in the UtopIR instruction set.
/// </summary>
/// <remarks>
/// <para>
/// Each value corresponds to a UtopIR bitwise instruction mnemonic and a Topsy Turvy bitwise
/// operator.  All operations here are binary: they consume exactly two integer operands and produce
/// one result.  The unary bitwise NOT is represented separately by <see cref="InvInstruction"/> and
/// is therefore not included in this enumeration..
/// </para>
/// <para>
/// For each UtopIR bitwise operation, there is a corresponding Topsy Turvy operator:
/// * <see cref="UtopIRBitwiseOperation"/><c>.Chord</c>: <c>chord</c> (<c>CHORD OF x AND y</c>)
/// * <see cref="UtopIRBitwiseOperation"/><c>.Harmony</c>: <c>harmony</c> (<c>HARMONY OF x AND y</c>)
/// * <see cref="UtopIRBitwiseOperation"/><c>.Discord</c>: <c>discord</c> (<c>DISCORD OF x AND y</c>)
/// * <see cref="UtopIRBitwiseOperation"/><c>.TransUp</c>: <c>transup</c> (<c>TRANSPOSITION UP x</c>)
/// * <see cref="UtopIRBitwiseOperation"/><c>.TransDown</c>: <c>transdown</c> (<c>TRANSPOSITION DOWN x</c>)
/// </para>
/// </remarks>
public enum UtopIRBitwiseOperation
{
    /// <summary>chord: Bitwise AND, CHORD OF x AND y</summary>
    Chord,

    /// <summary>harmony: Bitwise OR, HARMONY OF x AND y</summary>
    Harmony,

    /// <summary>discord: Bitwise XOR, DISCORD OF x AND y</summary>
    Discord,

    /// <summary>transup: Left Shift, TRANSPOSITION UP x</summary>
    TransUp,

    /// <summary>transdown: Right Shift, TRANSPOSITION DOWN x</summary>
    TransDown,
}
