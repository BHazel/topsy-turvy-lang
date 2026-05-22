namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Defines the supported literal types in the Topsy Turvy language.
/// </summary>
public enum TopsyTurvyLiteralType
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
    Null
}
