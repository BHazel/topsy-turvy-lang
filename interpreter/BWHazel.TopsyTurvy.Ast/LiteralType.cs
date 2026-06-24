namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Defines the supported literal types in the Topsy Turvy language.
/// </summary>
/// <remarks>
/// <para>
/// This enumeration represents the different types of literals that can be used in the Topsy Turvy language.  The different literal types include:
/// </para>
/// <para>
/// **Integer Types (Signed)**
/// * <see cref="LiteralType"/><c>.Long</c>: A 64-bit signed whole number, represented in Topsy Turvy as a <c>CHANCELLOR</c>, e.g. <c>1234567890123456789</c>.
/// * <see cref="LiteralType"/><c>.Integer</c>: A 32-bit signed whole number, represented in Topsy Turvy as a <c>PEER</c>, e.g. <c>20</c>.
/// * <see cref="LiteralType"/><c>.Short</c>: A 16-bit signed whole number, represented in Topsy Turvy as a <c>PIRATE</c>, e.g. <c>30000</c>.
/// * <see cref="LiteralType"/><c>.SignedByte</c>: An 8-bit signed whole number, represented in Topsy Turvy as a <c>SAUSAGE-ROLL</c>, e.g. <c>127</c>.
/// </para>
/// <para>
/// **Integer Types (Unsigned)**
/// Preceding the type with the <c>STANDING</c> modifier produces an unsigned variant.
/// * <see cref="LiteralType"/><c>.UnsignedLong</c>: A 64-bit unsigned whole number, represented as <c>STANDING CHANCELLOR</c>.
/// * <see cref="LiteralType"/><c>.UnsignedInteger</c>: A 32-bit unsigned whole number, represented as <c>STANDING PEER</c>.
/// * <see cref="LiteralType"/><c>.UnsignedShort</c>: A 16-bit unsigned whole number, represented as <c>STANDING PIRATE</c>.
/// * <see cref="LiteralType"/><c>.Byte</c>: An 8-bit unsigned whole number, represented as <c>STANDING SAUSAGE-ROLL</c>.
/// </para>
/// <para>
/// **Floating-Point Types**
/// * <see cref="LiteralType"/><c>.Double</c>: A 64-bit double-precision real number, represented as a <c>FATHOM</c>, e.g. <c>3.14</c>.
/// * <see cref="LiteralType"/><c>.Single</c>: A 32-bit single-precision real number, represented as a <c>FOOT</c>, e.g. <c>3.14</c>.
/// </para>
/// <para>
/// **Other Types**
/// * <see cref="LiteralType"/><c>.String</c>: A sequence of characters, represented in Topsy Turvy as a <c>YARN</c>, e.g. <c>"Hello, World!"</c>.
/// * <see cref="LiteralType"/><c>.Char</c>: A single character, represented in Topsy Turvy as a <c>STITCH</c>, e.g. <c>'A'</c>.
/// * <see cref="LiteralType"/><c>.Boolean</c>: A boolean value, represented in Topsy Turvy as a <c>DECREE</c>, e.g. <c>VERITY</c> or <c>NAY</c>.
/// * <see cref="LiteralType"/><c>.Null</c>: A null value, represented in Topsy Turvy as <c>NAUGHT</c>, e.g. <c>NAUGHT</c>.
/// * <see cref="LiteralType"/><c>.Array</c>: An ordered collection declared with <c>A LITTLE LIST OF &lt;type&gt;</c>.
/// </para>
/// <para>
/// ### Widening Rules
/// In arithmetic operations, variables of numeric types are automatically widened to the largest type involved in the following
/// order, from widest:
/// * <see cref="LiteralType"/><c>.Double</c> (<c>FATHOM</c>)
/// * <see cref="LiteralType"/><c>.Single</c> (<c>FOOT</c>)
/// * <see cref="LiteralType"/><c>.UnsignedLong</c> (<c>STANDING CHANCELLOR</c>)
/// * <see cref="LiteralType"/><c>.Long</c> (<c>CHANCELLOR</c>)
/// * <see cref="LiteralType"/><c>.UnsignedInteger</c> (<c>STANDING PEER</c>)
/// * <see cref="LiteralType"/><c>.Integer</c> (<c>PEER</c>)
/// * <see cref="LiteralType"/><c>.UnsignedShort</c> (<c>STANDING PIRATE</c>)
/// * <see cref="LiteralType"/><c>.Short</c> (<c>PIRATE</c>)
/// * <see cref="LiteralType"/><c>.UnsignedByte</c> (<c>STANDING SAUSAGE-ROLL</c>)
/// * <see cref="LiteralType"/><c>.SignedByte</c> (<c>SAUSAGE-ROLL</c>)
/// 
/// If any operand is a floating-point type, all operands are widened to the largest floating-point type in the operation.  For example,
/// if one operand is a <c>FATHOM</c> then the result is also a <c>FATHOM</c>.  If one operand is a <c>FOOT</c> and the other is not a
/// <c>FATHOM</c>, then the result is a <c>FOOT</c>.  If neither operand is a floating-point type, then the result is the largest integer
/// type in the operation.
/// </para>
/// </remarks>
public enum LiteralType
{
    /// <summary>A 32-bit signed whole number (<c>PEER</c>).</summary>
    Integer,

    /// <summary>A 64-bit signed whole number (<c>CHANCELLOR</c>).</summary>
    Long,

    /// <summary>A 16-bit signed whole number (<c>PIRATE</c>).</summary>
    Short,

    /// <summary>An 8-bit signed whole number (<c>SAUSAGE-ROLL</c>).</summary>
    SignedByte,

    /// <summary>A 32-bit unsigned whole number (<c>STANDING PEER</c>).</summary>
    UnsignedInteger,

    /// <summary>A 64-bit unsigned whole number (<c>STANDING CHANCELLOR</c>).</summary>
    UnsignedLong,

    /// <summary>A 16-bit unsigned whole number (<c>STANDING PIRATE</c>).</summary>
    UnsignedShort,

    /// <summary>An 8-bit unsigned whole number (<c>STANDING SAUSAGE-ROLL</c>).</summary>
    Byte,

    /// <summary>A 64-bit double-precision real number (<c>FATHOM</c>).</summary>
    Double,

    /// <summary>A 32-bit single-precision real number (<c>FOOT</c>).</summary>
    Single,

    /// <summary>A sequence of characters (<c>YARN</c>).</summary>
    String,

    /// <summary>A single character (<c>STITCH</c>); literal form <c>'A'</c>.</summary>
    Char,

    /// <summary>A boolean value (<c>DECREE</c>).</summary>
    Boolean,

    /// <summary>A null value (<c>NAUGHT</c>).</summary>
    Null,

    /// <summary>An ordered collection (<c>A LITTLE LIST OF &lt;type&gt;</c>).</summary>
    Array
}
