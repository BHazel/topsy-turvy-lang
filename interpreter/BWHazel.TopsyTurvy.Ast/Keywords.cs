namespace BWHazel.TopsyTurvy.Ast;

/// <summary>
/// Defines Topsy Turvy language keyword strings.
/// </summary>
/// <remarks>
/// These constants are provided for convenience and consistency to ensure that the same string literals are used where necessary.
/// However, they are not necessarily used throughout the entire codebase; this is intended for readability, especially in the Parser.
/// </remarks>
public static class Keywords
{
    /// <summary>
    /// Boolean and null literal keywords.
    /// </summary>
    public static class Literals
    {
        /// <summary>Boolean <c>true</c> literal.</summary>
        public const string Verity = "VERITY";

        /// <summary>Boolean <c>false</c> literal.</summary>
        public const string Nay = "NAY";

        /// <summary>Null literal and null type name.</summary>
        public const string Naught = "NAUGHT";
    }

    /// <summary>
    /// Type keywords for declarations and casts.
    /// </summary>
    public static class TypeNames
    {
        /// <summary>Integer type (<c>PEER</c>).</summary>
        public const string Peer = "PEER";

        /// <summary>Floating-point type (<c>FATHOM</c>).</summary>
        public const string Fathom = "FATHOM";

        /// <summary>String type (<c>YARN</c>).</summary>
        public const string Yarn = "YARN";

        /// <summary>Boolean type (<c>DECREE</c>).</summary>
        public const string Decree = "DECREE";

        /// <summary>Null type (<c>NAUGHT</c>).</summary>
        /// <remarks>
        /// The same token as the null literal, <see cref="Literals.Naught"/>.
        /// </remarks>
        public const string Naught = Literals.Naught;

        /// <summary>
        /// Array type annotation prefix (<c>LITTLE LIST OF</c>).
        /// </summary>
        public const string LittleListOf = "LITTLE LIST OF";
    }

    /// <summary>
    /// Special built-in keywords.
    /// </summary>
    public static class SpecialNames
    {
        /// <summary>The implicit variable.</summary>
        public const string JustSo = "JUST SO";

        /// <summary>The constant array of command-line arguments.</summary>
        public const string TheProps = "THE PROPS";
    }
}
