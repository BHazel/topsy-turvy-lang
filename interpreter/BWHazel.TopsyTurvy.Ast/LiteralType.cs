namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Defines the supported literal types in the Topsy Turvy language.
/// </summary>
/// <remarks>
/// <para>
/// This enumeration represents the different types of literals that can be used in the Topsy Turvy language.  The different literal types include:
/// * <see cref="LiteralType"/><c>.Integer</c>: A whole number, represented in Topsy Turvy as a PEER, e.g. <c>20</c>.
/// * <see cref="LiteralType"/><c>.Float</c>: A real number, represented in Topsy Turvy as a FATHOM, e.g. <c>3.14</c>.
/// * <see cref="LiteralType"/><c>.String</c>: A sequence of characters, represented in Topsy Turvy as a YARN, e.g. <c>"Hello, World!"</c>.
/// * <see cref="LiteralType"/><c>.Boolean</c>: A boolean value, represented in Topsy Turvy as a DECREE, e.g. <c>VERITY</c> or <c>NAY</c>.
/// * <see cref="LiteralType"/><c>.Null</c>: A null value, represented in Topsy Turvy as NAUGHT, e.g. <c>NAUGHT</c>.
/// * <see cref="LiteralType"/><c>.Array</c>: An ordered collection declared with <c>A LITTLE LIST OF &lt;type&gt;</c>.
/// </para>
/// </remarks>
public enum LiteralType
{
    /// <summary>A whole number (PEER).</summary>
    Integer,

    /// <summary>A real number (FATHOM).</summary>
    Float,

    /// <summary>A sequence of characters (YARN).</summary>
    String,

    /// <summary>A boolean value (DECREE).</summary>
    Boolean,

    /// <summary>A null value (NAUGHT).</summary>
    Null,

    /// <summary>An ordered collection (<c>A LITTLE LIST OF &lt;type&gt;</c>).</summary>
    Array
}
